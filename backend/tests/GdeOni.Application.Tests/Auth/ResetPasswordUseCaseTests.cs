using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.ResetPassword.Model;
using GdeOni.Application.Auth.ResetPassword.UseCase;
using GdeOni.Application.Auth.ResetPassword.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Tests.Auth;

/// <summary>
/// D43. Тесты <see cref="ResetPasswordUseCase"/>.
///
/// Ключевое, что защищаем: успешный сброс обязан закрыть ВСЕ активные
/// сессии (сценарий «аккаунт увели» — владелец возвращает контроль),
/// а битый/просроченный токен обязан давать понятную ошибку, а не
/// молчаливый успех.
/// </summary>
public sealed class ResetPasswordUseCaseTests
{
    private const string Email = "ivan@example.com";
    private const string OldHash = "old$hash$with$chars";
    private const string PlainToken = "plain-token";
    private const string NewPassword = "new-password-123";

    [Fact]
    public async Task Execute_ValidToken_ChangesPassword()
    {
        var user = CreateUserWithPendingReset();
        var (useCase, _, _, _) = Build(user);

        var result = await useCase.Execute(
            new ResetPasswordCommand(PlainToken, NewPassword), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be($"hashed::{NewPassword}");
    }

    /// <summary>
    /// Сброс закрывает все сессии: revoke refresh-токенов + сброс кеша
    /// SecurityStamp. Без этого угонщик остался бы в аккаунте.
    /// </summary>
    [Fact]
    public async Task Execute_ValidToken_RevokesSessionsEverywhere()
    {
        var user = CreateUserWithPendingReset();
        var (useCase, userRepo, refreshRepo, invalidator) = Build(user);

        await useCase.Execute(new ResetPasswordCommand(PlainToken, NewPassword), default);

        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        invalidator.Verify(x => x.Invalidate(user.Id), Times.Once);
    }

    [Fact]
    public async Task Execute_UnknownToken_ReturnsInvalid()
    {
        var (useCase, _, refreshRepo, _) = Build(user: null);

        var result = await useCase.Execute(
            new ResetPasswordCommand(PlainToken, NewPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.invalid");
        refreshRepo.Verify(
            x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_ExpiredToken_ReturnsExpired()
    {
        var user = CreateUser();
        // Токен, срок которого уже истёк на момент проверки.
        user.RequestPasswordReset(
            $"hash::{PlainToken}",
            DateTime.UtcNow.AddMinutes(1),
            DateTime.UtcNow);
        var (useCase, _, _, _) = Build(user, now: DateTime.UtcNow.AddHours(2));

        var result = await useCase.Execute(
            new ResetPasswordCommand(PlainToken, NewPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.expired");
        user.PasswordHash.Should().Be(OldHash);
    }

    [Fact]
    public async Task Execute_BlockedUser_ReturnsInvalid()
    {
        var user = CreateUserWithPendingReset();
        user.Block(Guid.NewGuid(), "спам", DateTime.UtcNow);
        var (useCase, _, _, _) = Build(user);

        var result = await useCase.Execute(
            new ResetPasswordCommand(PlainToken, NewPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.invalid");
    }

    [Fact]
    public async Task Execute_TooShortPassword_ReturnsValidationError()
    {
        var user = CreateUserWithPendingReset();
        var (useCase, _, _, _) = Build(user);

        var result = await useCase.Execute(
            new ResetPasswordCommand(PlainToken, "short"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Details.Should().Contain(e => e.ErrorCode == "user.password.too_short");
    }

    [Fact]
    public async Task Execute_EmptyToken_ReturnsValidationError()
    {
        var (useCase, _, _, _) = Build(user: null);

        var result = await useCase.Execute(
            new ResetPasswordCommand(string.Empty, NewPassword), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Details.Should()
            .Contain(e => e.ErrorCode == "user.password_reset_token.invalid");
    }

    private static (
        IResetPasswordUseCase UseCase,
        Mock<IUserRepository> UserRepo,
        Mock<IRefreshTokenRepository> RefreshRepo,
        Mock<ISecurityStampInvalidator> Invalidator)
        Build(User? user, DateTime? now = null)
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo
            .Setup(x => x.GetByPasswordResetTokenHash(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.Save(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var refreshRepo = new Mock<IRefreshTokenRepository>();
        refreshRepo
            .Setup(x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tokenFactory = new Mock<ISecureTokenFactory>();
        tokenFactory.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(t => $"hash::{t}");

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(p => $"hashed::{p}");

        var invalidator = new Mock<ISecurityStampInvalidator>();

        var timeProvider = now is null
            ? TimeProvider.System
            : new FixedTimeProvider(now.Value);

        var useCase = new ResetPasswordUseCase(
            userRepo.Object,
            refreshRepo.Object,
            tokenFactory.Object,
            hasher.Object,
            invalidator.Object,
            TestExecutor.With<ResetPasswordCommand, ResetPasswordCommandValidator>(),
            timeProvider);

        return (useCase, userRepo, refreshRepo, invalidator);
    }

    private static User CreateUser() =>
        User.Register(email: Email, passwordHash: OldHash).Value;

    private static User CreateUserWithPendingReset()
    {
        var user = CreateUser();
        var now = DateTime.UtcNow;
        user.RequestPasswordReset($"hash::{PlainToken}", now.AddHours(1), now);
        return user;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
