using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Application.Users.Commands.UpdateProfile.UseCase;
using GdeOni.Application.Users.Commands.UpdateProfile.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="UpdateUserProfileUseCase"/>: self/admin → success,
/// outsider → UserForbidden, дубль userName → UserNameAlreadyExists.
/// </summary>
public sealed class UpdateUserProfileUseCaseTests
{
    /// <summary>
    /// Self обновляет свой профиль → success + Save вызван.
    /// </summary>
    [Fact]
    public async Task Execute_Self_Succeeds()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await useCase.Execute(
            new UpdateUserProfileCommand(user.Id, "alice2", "Иван Иванов"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        // D11.8.1: реальная смена UserName ротировала stamp → invalidate.
        invalidator.Verify(x => x.Invalidate(user.Id), Times.Once);
    }

    /// <summary>
    /// D11.8.2: PATCH с теми же UserName/FullName — domain делает no-op,
    /// SecurityStamp не ротируется, Invalidate НЕ вызывается.
    /// Save всё равно вызывается (use case не оптимизирует это, EF
    /// сам определяет отсутствие изменений в ChangeTracker).
    /// </summary>
    [Fact]
    public async Task Execute_SameValues_DoesNotInvalidate()
    {
        var user = User.Register(
            "alice@example.com", "hash",
            fullName: "Alice Cooper", userName: "Alice").Value;
        var oldStamp = user.SecurityStamp;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new UpdateUserProfileCommand(user.Id, "Alice", "Alice Cooper"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.SecurityStamp.Should().Be(oldStamp);
        invalidator.Verify(x => x.Invalidate(It.IsAny<Guid>()), Times.Never);
    }

    /// <summary>
    /// Admin может обновить чужой профиль → success.
    /// </summary>
    [Fact]
    public async Task Execute_Admin_OnAnotherUser_Succeeds()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        userRepo.Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await useCase.Execute(
            new UpdateUserProfileCommand(target.Id, "newName", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Outsider (не self, не admin) → UserForbidden.
    /// </summary>
    [Fact]
    public async Task Execute_Outsider_ReturnsUserForbidden()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new UpdateUserProfileCommand(target.Id, "hacked", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    /// <summary>
    /// userName уже занят другим юзером (нормализованный != нынешнего) →
    /// UserNameAlreadyExists.
    /// </summary>
    [Fact]
    public async Task Execute_UserNameAlreadyTakenByAnother_ReturnsConflict()
    {
        var user = User.Register("alice@example.com", "hash", userName: "alice").Value;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.ExistsByUserName("bob", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new UpdateUserProfileCommand(user.Id, "bob", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.user_name.already.exists");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        Mock<ISecurityStampInvalidator> Invalidator,
        UpdateUserProfileUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var invalidator = new Mock<ISecurityStampInvalidator>();
        var useCase = new UpdateUserProfileUseCase(
            userRepo.Object,
            refreshRepo.Object,
            currentUser.Object,
            passwordHasher.Object,
            invalidator.Object,
            TestExecutor.With<UpdateUserProfileCommand, UpdateUserProfileCommandValidator>());
        return (userRepo, currentUser, invalidator, useCase);
    }
}
