using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Application.Users.Commands.Register.UseCase;
using GdeOni.Application.Users.Commands.Register.Validation;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="RegisterUserUseCase"/>: уникальность email/userName,
/// хеширование пароля, корректное создание User через домен-фабрику.
/// </summary>
public sealed class RegisterUserUseCaseTests
{
    /// <summary>
    /// Дубликат email → EmailAlreadyExists. ExistsByEmail предварительно
    /// возвращает true; до Save и Add дело не доходит.
    /// </summary>
    [Fact]
    public async Task Execute_DuplicateEmail_ReturnsEmailAlreadyExists()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new RegisterUserCommand("john@example.com", null, null, "Password123!", true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email.already.exists");
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Дубликат userName → UserNameAlreadyExists. ExistsByEmail false,
    /// ExistsByUserName true.
    /// </summary>
    [Fact]
    public async Task Execute_DuplicateUserName_ReturnsUserNameAlreadyExists()
    {
        var (userRepo, _, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new RegisterUserCommand("alice@example.com", "alice", null, "Password123!", true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.user_name.already.exists");
    }

    /// <summary>
    /// Happy path: ExistsByEmail/UserName=false → Hash вызывается,
    /// User.Register создаётся, Add+Save вызываются.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_AddsUserAndSaves()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash("Password123!")).Returns("hash-bcrypt");

        var result = await useCase.Execute(
            new RegisterUserCommand("new@example.com", "newuser", "Иван Иванов", "Password123!", true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
        hasher.Verify(x => x.Hash("Password123!"), Times.Once);
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<IPasswordHasher> Hasher,
        RegisterUserUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        // D16: RegisterUserUseCase теперь дёргает StartTrial и читает
        // длительность из SubscriptionOptions через IOptions.
        var subscriptionOptions = Microsoft.Extensions.Options.Options.Create(
            new GdeOni.Application.Subscriptions.SubscriptionOptions());
        // D19: RegisterUserUseCase также читает текущие версии Privacy/Terms
        // из LegalOptions и вызывает User.AcceptLegal.
        var legalOptions = Microsoft.Extensions.Options.Options.Create(
            new GdeOni.Application.Legal.LegalOptions());
        var useCase = new RegisterUserUseCase(
            userRepo.Object,
            hasher.Object,
            TestExecutor.With<RegisterUserCommand, RegisterUserCommandValidator>(),
            subscriptionOptions,
            legalOptions);
        return (userRepo, hasher, useCase);
    }
}
