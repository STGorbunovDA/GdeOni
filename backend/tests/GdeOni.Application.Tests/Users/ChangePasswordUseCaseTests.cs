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

        var (userRepo, refreshRepo, hasher, currentUser, invalidator, useCase) =
            BuildHarness(currentUserId: userId, isAdmin: false);

        userRepo
            .Setup(x => x.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        // CurrentPassword совпадает.
        hasher
            .Setup(x => x.Verify("OldPassword123!", user.PasswordHash))
            .Returns(true);
        // No-op detect (D11.8.2): новый пароль НЕ совпадает с текущим.
        hasher
            .Setup(x => x.Verify("NewPassword456!", user.PasswordHash))
            .Returns(false);
        hasher
            .Setup(x => x.Hash("NewPassword456!"))
            .Returns("new-hash");

        // Act
        var result = await useCase.Execute(
            new ChangePasswordCommand(userId, "OldPassword123!", "NewPassword456!"),
            CancellationToken.None);

        // Assert: успех + хеш обновлён + Save + RevokeAllForUser + Invalidate.
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        invalidator.Verify(x => x.Invalidate(userId), Times.Once);
    }

    /// <summary>
    /// D11.8.2: новый пароль идентичен текущему — Save / Hash /
    /// RevokeAllForUser / Invalidate НЕ вызываются.
    /// </summary>
    [Fact]
    public async Task Execute_NewPasswordSameAsCurrent_NoOp()
    {
        var user = User.Register("self@example.com", "current-hash").Value;
        var userId = user.Id;

        var (userRepo, refreshRepo, hasher, _, invalidator, useCase) =
            BuildHarness(currentUserId: userId, isAdmin: false);

        userRepo
            .Setup(x => x.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        hasher
            .Setup(x => x.Verify("SamePassword123!", user.PasswordHash))
            .Returns(true);

        var result = await useCase.Execute(
            new ChangePasswordCommand(userId, "SamePassword123!", "SamePassword123!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("current-hash");
        hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        invalidator.Verify(x => x.Invalidate(It.IsAny<Guid>()), Times.Never);
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

        var (userRepo, refreshRepo, hasher, _, _, useCase) =
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

        var (userRepo, refreshRepo, hasher, _, _, useCase) =
            BuildHarness(currentUserId: adminId, isAdmin: true);

        var originalHash = victim.PasswordHash;
        userRepo
            .Setup(x => x.GetById(victimId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(victim);
        // No-op detect: новый пароль не совпадает с текущим хешем.
        hasher
            .Setup(x => x.Verify("AdminReset123!", originalHash))
            .Returns(false);
        hasher
            .Setup(x => x.Hash("AdminReset123!"))
            .Returns("new-hash");

        var result = await useCase.Execute(
            new ChangePasswordCommand(victimId, CurrentPassword: null, "AdminReset123!"),
            CancellationToken.None);

        // Assert: успех + Verify CurrentPassword НЕ вызывался (skip).
        // Verify нового пароля для no-op detect — вызывается ровно один раз
        // против исходного хеша до его перезаписи.
        result.IsSuccess.Should().BeTrue();
        victim.PasswordHash.Should().Be("new-hash");
        hasher.Verify(
            x => x.Verify("AdminReset123!", originalHash),
            Times.Once);
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
        Mock<ISecurityStampInvalidator>,
        ChangePasswordUseCase) BuildHarness(Guid currentUserId, bool isAdmin)
    {
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var currentUser = new Mock<ICurrentUserService>();
        var invalidator = new Mock<ISecurityStampInvalidator>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);

        var useCase = new ChangePasswordUseCase(
            userRepo.Object,
            refreshRepo.Object,
            hasher.Object,
            currentUser.Object,
            invalidator.Object,
            TestExecutor.With<ChangePasswordCommand, ChangePasswordCommandValidator>());

        return (userRepo, refreshRepo, hasher, currentUser, invalidator, useCase);
    }
}
