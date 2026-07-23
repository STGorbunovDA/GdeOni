using GdeOni.Domain.Aggregates.User;

// Namespace как в UserTests — иначе `User` резолвится в локальный
// namespace вместо агрегата.
namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// D43. Доменные инварианты восстановления пароля по ссылке из письма.
/// Ключевые свойства, которые тут защищаем: токен одноразовый, срок
/// действия соблюдается, успешный сброс закрывает все сессии, а обычная
/// смена пароля/email гасит ранее выданную ссылку.
/// </summary>
public sealed class UserPasswordResetTests
{
    private const string SampleEmail = "ivan@example.com";
    private const string SamplePasswordHash = "hash$with$enough$chars";
    private const string NewPasswordHash = "new$hash$with$chars";
    private const string TokenHash = "a1b2c3d4e5f6";

    private static readonly DateTime Now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequestPasswordReset_StoresHashAndExpiry()
    {
        var user = CreateSampleUser();

        var result = user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetTokenHash.Should().Be(TokenHash);
        user.PasswordResetTokenExpiresAtUtc.Should().Be(Now.AddHours(1));
    }

    /// <summary>
    /// Запрос сброса сам по себе прав не меняет — разлогинивать человека,
    /// который просто нажал «забыли пароль», незачем. Иначе достаточно
    /// было бы знать чужой email, чтобы выкидывать его из аккаунта.
    /// </summary>
    [Fact]
    public void RequestPasswordReset_DoesNotRotateSecurityStamp()
    {
        var user = CreateSampleUser();
        var stampBefore = user.SecurityStamp;

        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        user.SecurityStamp.Should().Be(stampBefore);
    }

    /// <summary>
    /// Повторный запрос обязан перезаписать токен: сценарий «письмо не
    /// пришло, жму ещё раз» не должен присылать мёртвую ссылку.
    /// </summary>
    [Fact]
    public void RequestPasswordReset_Twice_OverwritesPreviousToken()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        user.RequestPasswordReset("second-token-hash", Now.AddHours(2), Now);

        user.PasswordResetTokenHash.Should().Be("second-token-hash");
        user.PasswordResetTokenExpiresAtUtc.Should().Be(Now.AddHours(2));
    }

    [Fact]
    public void RequestPasswordReset_AlreadyExpiredWindow_IsRejected()
    {
        var user = CreateSampleUser();

        var result = user.RequestPasswordReset(TokenHash, Now.AddMinutes(-1), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.expired");
    }

    [Fact]
    public void ResetPasswordByToken_ValidToken_ChangesPassword()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        var result = user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be(NewPasswordHash);
    }

    /// <summary>
    /// Сброс закрывает все активные сессии. Сценарий «аккаунт увели»:
    /// владелец восстанавливает пароль и тем самым выкидывает чужого.
    /// </summary>
    [Fact]
    public void ResetPasswordByToken_RotatesSecurityStamp()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);
        var stampBefore = user.SecurityStamp;

        user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now);

        user.SecurityStamp.Should().NotBe(stampBefore);
    }

    /// <summary>Одноразовость: по одной ссылке нельзя пройти дважды.</summary>
    [Fact]
    public void ResetPasswordByToken_SecondUse_IsRejected()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);
        user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now);

        var second = user.ResetPasswordByToken(TokenHash, "another$hash", Now);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("user.password_reset_token.invalid");
        user.PasswordHash.Should().Be(NewPasswordHash);
    }

    [Fact]
    public void ResetPasswordByToken_WrongToken_IsRejected()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        var result = user.ResetPasswordByToken("not-the-right-hash", NewPasswordHash, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.invalid");
        user.PasswordHash.Should().Be(SamplePasswordHash);
    }

    [Fact]
    public void ResetPasswordByToken_ExpiredToken_IsRejected()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        var result = user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.expired");
        user.PasswordHash.Should().Be(SamplePasswordHash);
    }

    [Fact]
    public void ResetPasswordByToken_WithoutRequest_IsRejected()
    {
        var user = CreateSampleUser();

        var result = user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_reset_token.invalid");
    }

    /// <summary>
    /// Вспомнил пароль и сменил сам — старое письмо перестаёт быть
    /// ключом к аккаунту.
    /// </summary>
    [Fact]
    public void ChangePasswordHash_ClearsPendingResetToken()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        user.ChangePasswordHash("changed$manually$hash");

        user.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetTokenExpiresAtUtc.Should().BeNull();

        var afterChange = user.ResetPasswordByToken(TokenHash, NewPasswordHash, Now);
        afterChange.IsFailure.Should().BeTrue();
    }

    /// <summary>
    /// Ссылка ушла на СТАРЫЙ адрес — после смены email прежний ящик
    /// не должен оставаться входом в аккаунт.
    /// </summary>
    [Fact]
    public void ChangeEmail_ClearsPendingResetToken()
    {
        var user = CreateSampleUser();
        user.RequestPasswordReset(TokenHash, Now.AddHours(1), Now);

        user.ChangeEmail("new-address@example.com");

        user.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetTokenExpiresAtUtc.Should().BeNull();
    }

    private static User CreateSampleUser() =>
        User.Register(email: SampleEmail, passwordHash: SamplePasswordHash).Value;
}
