using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.ChangeLogin.Model;
using GdeOni.Application.Users.Commands.ChangeLogin.UseCase;
using GdeOni.Application.Users.Commands.ChangeLogin.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Смена собственного логина. Ключевой инвариант: двух одинаковых логинов в
/// системе быть не может — занятый чужим отбивается ДО записи.
/// </summary>
public sealed class ChangeLoginUseCaseTests
{
    [Fact]
    public async Task Execute_LoginTakenByAnotherUser_ReturnsConflictAndDoesNotSave()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var me = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(me));

        // «bous07» уже занят другим пользователем (bous07@mail.ru).
        userRepo
            .Setup(x => x.ExistsByLoginExceptUser("bous07", me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new ChangeLoginCommand("bous07"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.login.already.exists");
        // Ничего не сохранили: логин остался прежним.
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_FreeLogin_ChangesAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var me = Guid.NewGuid();
        var user = User.Register("bous07@yandex.ru", "$hash").Value;
        user.Login.Should().Be("bous07"); // сгенерирован из префикса

        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(me));
        userRepo
            .Setup(x => x.ExistsByLoginExceptUser(It.IsAny<string>(), me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.GetById(me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ChangeLoginCommand("  BousNew  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Login.Should().Be("bousnew"); // trim + lowercase
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Свой же текущий логин занятым не считается — иначе «сохранить без
    /// изменений» отбивалось бы как конфликт. Проверка идёт с исключением
    /// себя, поэтому use case доходит до домена, где срабатывает no-op guard.
    /// </summary>
    [Fact]
    public async Task Execute_SameLoginAsOwn_Succeeds()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var me = Guid.NewGuid();
        var user = User.Register("bous07@yandex.ru", "$hash").Value;

        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(me));
        userRepo
            .Setup(x => x.ExistsByLoginExceptUser("bous07", me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.GetById(me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ChangeLoginCommand("bous07"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Login.Should().Be("bous07");
    }

    [Fact]
    public async Task Execute_InvalidLogin_ReturnsValidationError()
    {
        var (_, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var result = await useCase.Execute(
            new ChangeLoginCommand("Иван Петров"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.login.invalid");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        ChangeLoginUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var useCase = new ChangeLoginUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<ChangeLoginCommand, ChangeLoginCommandValidator>());
        return (userRepo, currentUser, useCase);
    }
}
