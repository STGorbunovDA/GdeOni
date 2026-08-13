using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.AssignMissingLogins.Model;
using GdeOni.Application.Users.Commands.AssignMissingLogins.UseCase;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Кнопка SuperAdmin «проставить логины тем, у кого их нет».
///
/// Главный риск — коллизии ВНУТРИ одного прохода: два адреса с одинаковым
/// префиксом (bous07@mail.ru и bous07@yandex.ru) не должны получить один и
/// тот же логин. Поэтому мок репозитория здесь stateful: он держит набор уже
/// занятых логинов, как это делает БД.
/// </summary>
public sealed class AssignMissingLoginsUseCaseTests
{
    [Fact]
    public async Task Execute_TwoSamePrefixes_SecondGetsFullEmail()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assigned = new Dictionary<Guid, string>();

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var (userRepo, useCase) = BuildHarness(
            taken,
            assigned,
            pending: new List<(Guid, string)>
            {
                (first, "bous07@mail.ru"),
                (second, "bous07@yandex.ru"),
            });

        var result = await useCase.Execute(
            new AssignMissingLoginsCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedCount.Should().Be(2);

        // Первый занял короткий префикс, второму достался полный адрес.
        assigned[first].Should().Be("bous07");
        assigned[second].Should().Be("bous07@yandex.ru");
        assigned.Values.Should().OnlyHaveUniqueItems();

        userRepo.Verify(
            x => x.SetLoginById(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Префикс уже занят ДРУГИМ, давно зарегистрированным пользователем
    /// (его в выборке «без логина» нет) — новичок получает полный email.
    /// </summary>
    [Fact]
    public async Task Execute_PrefixTakenByExistingUser_AssignsFullEmail()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bous07" };
        var assigned = new Dictionary<Guid, string>();
        var userId = Guid.NewGuid();

        var (_, useCase) = BuildHarness(
            taken,
            assigned,
            pending: new List<(Guid, string)> { (userId, "bous07@yandex.ru") });

        var result = await useCase.Execute(
            new AssignMissingLoginsCommand(),
            CancellationToken.None);

        result.Value.AssignedCount.Should().Be(1);
        assigned[userId].Should().Be("bous07@yandex.ru");
    }

    /// <summary>Идемпотентность: пустых логинов нет — 0 и ни одного UPDATE.</summary>
    [Fact]
    public async Task Execute_NobodyWithoutLogin_ReturnsZero()
    {
        var (userRepo, useCase) = BuildHarness(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<Guid, string>(),
            pending: new List<(Guid, string)>());

        var result = await useCase.Execute(
            new AssignMissingLoginsCommand(),
            CancellationToken.None);

        result.Value.AssignedCount.Should().Be(0);
        userRepo.Verify(
            x => x.SetLoginById(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_NotSuperAdmin_ReturnsForbidden()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(false);

        var useCase = new AssignMissingLoginsUseCase(userRepo.Object, currentUser.Object);

        var result = await useCase.Execute(
            new AssignMissingLoginsCommand(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
        userRepo.Verify(
            x => x.GetUsersWithoutLogin(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Stateful-мок: ExistsByLogin смотрит в набор занятых, SetLoginById
    /// пополняет его — ровно как БД в пределах одного прохода.
    /// </summary>
    private static (Mock<IUserRepository> UserRepo, AssignMissingLoginsUseCase UseCase) BuildHarness(
        HashSet<string> taken,
        Dictionary<Guid, string> assigned,
        List<(Guid Id, string Email)> pending)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(true);

        userRepo
            .Setup(x => x.GetUsersWithoutLogin(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        userRepo
            .Setup(x => x.ExistsByLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string login, CancellationToken _) => taken.Contains(login));

        userRepo
            .Setup(x => x.SetLoginById(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string login, CancellationToken _) =>
            {
                taken.Add(login);
                assigned[id] = login;
                return 1;
            });

        return (userRepo, new AssignMissingLoginsUseCase(userRepo.Object, currentUser.Object));
    }
}
