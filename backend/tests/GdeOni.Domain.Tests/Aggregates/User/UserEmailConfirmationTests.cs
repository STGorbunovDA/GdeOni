using GdeOni.Domain.Aggregates.User;

// Namespace как в UserTests — иначе `User` резолвится в локальный
// namespace вместо агрегата.
namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// D45. Доменные инварианты подтверждения email. Защищаем: токен
/// одноразовый, срок соблюдается, повторный клик по подтверждённому — no-op
/// (а не ошибка), смена email сбрасывает подтверждение, новая регистрация
/// подпадает под гейт, а сид-админ — нет.
/// </summary>
public sealed class UserEmailConfirmationTests
{
    private const string SampleEmail = "ivan@example.com";
    private const string SamplePasswordHash = "hash$with$enough$chars";
    private const string TokenHash = "a1b2c3d4e5f6";

    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly AdultBirthDate = new(2000, 1, 1);

    [Fact]
    public void RequestEmailConfirmation_StoresHashAndExpiry()
    {
        var user = CreateSampleUser();

        var result = user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmationTokenHash.Should().Be(TokenHash);
        user.EmailConfirmationTokenExpiresAtUtc.Should().Be(Now.AddHours(48));
        user.IsEmailConfirmed.Should().BeFalse();
    }

    /// <summary>
    /// Запрос подтверждения прав не меняет — разлогинивать никого не нужно.
    /// </summary>
    [Fact]
    public void RequestEmailConfirmation_DoesNotRotateSecurityStamp()
    {
        var user = CreateSampleUser();
        var stampBefore = user.SecurityStamp;

        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        user.SecurityStamp.Should().Be(stampBefore);
    }

    [Fact]
    public void RequestEmailConfirmation_Twice_OverwritesPreviousToken()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        user.RequestEmailConfirmation("second-token-hash", Now.AddHours(72), Now);

        user.EmailConfirmationTokenHash.Should().Be("second-token-hash");
        user.EmailConfirmationTokenExpiresAtUtc.Should().Be(Now.AddHours(72));
    }

    /// <summary>Уже подтверждён — новый токен не выписываем (no-op Success).</summary>
    [Fact]
    public void RequestEmailConfirmation_AlreadyConfirmed_IsNoOp()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);
        user.ConfirmEmailByToken(TokenHash, Now);

        var result = user.RequestEmailConfirmation("new-token", Now.AddHours(48), Now);

        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmationTokenHash.Should().BeNull();
    }

    [Fact]
    public void RequestEmailConfirmation_AlreadyExpiredWindow_IsRejected()
    {
        var user = CreateSampleUser();

        var result = user.RequestEmailConfirmation(TokenHash, Now.AddMinutes(-1), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email_confirmation_token.expired");
    }

    [Fact]
    public void ConfirmEmailByToken_ValidToken_ConfirmsAndClearsToken()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        var result = user.ConfirmEmailByToken(TokenHash, Now);

        result.IsSuccess.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        user.EmailConfirmedAtUtc.Should().Be(Now);
        user.EmailConfirmationTokenHash.Should().BeNull();
        user.EmailConfirmationTokenExpiresAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Подтверждение НЕ ротирует SecurityStamp — прав не меняет, активные
    /// сессии закрывать незачем (в отличие от сброса пароля).
    /// </summary>
    [Fact]
    public void ConfirmEmailByToken_DoesNotRotateSecurityStamp()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);
        var stampBefore = user.SecurityStamp;

        user.ConfirmEmailByToken(TokenHash, Now);

        user.SecurityStamp.Should().Be(stampBefore);
    }

    /// <summary>
    /// Клик по ссылке дважды: после первого раза токен погашен, но второй
    /// клик отвечает Success (адрес уже подтверждён) — не пугаем ошибкой.
    /// </summary>
    [Fact]
    public void ConfirmEmailByToken_SecondUse_IsIdempotentSuccess()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);
        user.ConfirmEmailByToken(TokenHash, Now);

        var second = user.ConfirmEmailByToken(TokenHash, Now);

        second.IsSuccess.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmailByToken_WrongToken_IsRejected()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        var result = user.ConfirmEmailByToken("not-the-right-hash", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email_confirmation_token.invalid");
        user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ConfirmEmailByToken_ExpiredToken_IsRejected()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);

        var result = user.ConfirmEmailByToken(TokenHash, Now.AddHours(49));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email_confirmation_token.expired");
        user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ConfirmEmailByToken_WithoutRequest_IsRejected()
    {
        var user = CreateSampleUser();

        var result = user.ConfirmEmailByToken(TokenHash, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email_confirmation_token.invalid");
    }

    /// <summary>
    /// Смена email сбрасывает подтверждение и гасит токен: новый адрес ещё
    /// никто не подтверждал, а старая ссылка вела на прежний ящик.
    /// </summary>
    [Fact]
    public void ChangeEmail_ResetsConfirmationAndClearsToken()
    {
        var user = CreateSampleUser();
        user.RequestEmailConfirmation(TokenHash, Now.AddHours(48), Now);
        user.ConfirmEmailByToken(TokenHash, Now);
        user.IsEmailConfirmed.Should().BeTrue();

        user.ChangeEmail("new-address@example.com");

        user.IsEmailConfirmed.Should().BeFalse();
        user.EmailConfirmedAtUtc.Should().BeNull();
        user.EmailConfirmationTokenHash.Should().BeNull();
    }

    /// <summary>Новая регистрация подпадает под гейт (Required=true), не подтверждена.</summary>
    [Fact]
    public void Register_NewUser_RequiresConfirmationAndIsUnconfirmed()
    {
        var user = User.Register(SampleEmail, SamplePasswordHash, AdultBirthDate, Now).Value;

        user.EmailConfirmationRequired.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeFalse();
    }

    /// <summary>Legacy-фабрика (тесты/история) под гейт не попадает.</summary>
    [Fact]
    public void Register_Legacy_DoesNotRequireConfirmation()
    {
        var user = CreateSampleUser();

        user.EmailConfirmationRequired.Should().BeFalse();
        user.IsEmailConfirmed.Should().BeFalse();
    }

    /// <summary>Сид-админ предподтверждён и под гейт не попадает.</summary>
    [Fact]
    public void RegisterSuperAdmin_IsPreconfirmed()
    {
        var admin = User.RegisterSuperAdmin(SampleEmail, SamplePasswordHash).Value;

        admin.IsEmailConfirmed.Should().BeTrue();
        admin.EmailConfirmationRequired.Should().BeFalse();
    }

    private static User CreateSampleUser() =>
        User.Register(email: SampleEmail, passwordHash: SamplePasswordHash).Value;
}
