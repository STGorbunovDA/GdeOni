using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Application.Users.Commands.ChangePassword.UseCase;
using GdeOni.Application.Users.Commands.ChangePassword.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="ChangePasswordUseCase"/> — критичный auth-сценарий.
/// Покрываем три ветки прав:
/// 1) self с правильным CurrentPassword → success + RevokeAllForUser;
/// 2) self с неправильным CurrentPassword → CurrentPasswordInvalid;
/// 3) admin сбрасывает чужой пароль БЕЗ проверки CurrentPassword.
///
/// Ключевой инвариант: после смены пароля все refresh-токены этого
/// пользователя обязаны быть отозваны (RevokeAllForUser), иначе
/// злоумышленник со старым refresh продолжит выпускать access-токены.
/// </summary>
public sealed class ChangePasswordUseCaseTests
{
    private static readonly Guid SelfId = Guid.NewGuid();

    /// <summary>
    /// Self с правильным CurrentPassword: успех + новый пароль
    /// захеширован + Save вызван + ВСЕ refresh-токены отозваны.
    /// </summary>
    [Fact]
    public async Task Execute_SelfWithCorrectCurrentPassword_RotatesPasswordAndRevokesTokens()
    {
        // Arrange
        var user = User.Register("self@example.com", "old-hash").Value;
        // Manually выставляем Id чтобы соответствовало SelfId — User.Id
        // приватный setter, поэтому используем то, что родилось.
        var userId = user.Id;

        var (userRepo, refreshRepo, hasher, currentUser, useCase) =
            BuildHarness(currentUserId: userId, isAdmin: false);

        userRepo
            .Setup(x => x.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        // CurrentPassword совпадает.
        hasher
            .Setup(x => x.Verify("OldPassword123!", user.PasswordHash))
            .Returns(true);
        hasher
            .Setup(x => x.Hash("NewPassword456!"))
            .Returns("new-hash");

        // Act
        var result = await useCase.Execute(
            new ChangePasswordCommand(userId, "OldPassword123!", "NewPassword456!"),
            CancellationToken.None);

        // Assert: успех + хеш обновлён + Save + RevokeAllForUser.
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Self с неправильным CurrentPassword → CurrentPasswordInvalid.
    /// Это 401 на API. Save и RevokeAllForUser НЕ вызываются —
    /// неудачная попытка не должна логаутить пользователя.
    /// </summary>
    [Fact]
    public async Task Execute_SelfWithWrongCurrentPassword_ReturnsCurrentPasswordInvalid()
    {
        var user = User.Register("self@example.com", "old-hash").Value;
        var userId = user.Id;

        var (userRepo, refreshRepo, hasher, _, useCase) =
            BuildHarness(currentUserId: userId, isAdmin: false);

        userRepo
            .Setup(x => x.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        hasher
            .Setup(x => x.Verify("WrongOld", user.PasswordHash))
            .Returns(false); // ← не совпадает.

        var result = await useCase.Execute(
            new ChangePasswordCommand(userId, "WrongOld", "NewPassword456!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.current_password.invalid");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Admin сбрасывает чужой пароль (cмена другого пользователя).
    /// CurrentPassword — null/любой; админ не знает чужой пароль,
    /// поэтому проверка CurrentPassword пропускается. RevokeAllForUser
    /// всё равно вызван — у жертвы старые токены отзываются.
    /// </summary>
    [Fact]
    public async Task Execute_AdminResettingOtherUserPassword_SkipsCurrentPasswordCheck()
    {
        var adminId = Guid.NewGuid();
        var victim = User.Register("victim@example.com", "old-hash").Value;
        var victimId = victim.Id;

        var (userRepo, refreshRepo, hasher, _, useCase) =
            BuildHarness(currentUserId: adminId, isAdmin: true);

        userRepo
            .Setup(x => x.GetById(victimId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(victim);
        hasher
            .Setup(x => x.Hash("AdminReset123!"))
            .Returns("new-hash");

        var result = await useCase.Execute(
            new ChangePasswordCommand(victimId, CurrentPassword: null, "AdminReset123!"),
            CancellationToken.None);

        // Assert: успех + Verify НЕ вызывался (skip CurrentPassword).
        result.IsSuccess.Should().BeTrue();
        victim.PasswordHash.Should().Be("new-hash");
        hasher.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(victimId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Helper: строит harness с моками + сконфигурированный use case.
    /// </summary>
    private static (
        Mock<IUserRepository>,
        Mock<IRefreshTokenRepository>,
        Mock<IPasswordHasher>,
        Mock<ICurrentUserService>,
        ChangePasswordUseCase) BuildHarness(Guid currentUserId, bool isAdmin)
    {
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);

        var useCase = new ChangePasswordUseCase(
            userRepo.Object,
            refreshRepo.Object,
            hasher.Object,
            currentUser.Object,
            TestExecutor.With<ChangePasswordCommand, ChangePasswordCommandValidator>());

        return (userRepo, refreshRepo, hasher, currentUser, useCase);
    }
}
