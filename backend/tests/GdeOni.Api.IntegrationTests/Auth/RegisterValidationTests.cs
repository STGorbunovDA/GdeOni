using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// Тесты валидации POST /api/users — registry-эндпоинт.
/// Покрываем все ошибки FluentValidation: невалидный email,
/// слишком короткий/длинный пароль, дубликат email.
/// Все возвращают 400 + конкретный validation-error code в Errors[].
///
/// Тесты требуют запущенный Docker (Postgres контейнер).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterValidationTests
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RegisterValidationTests(GdeOniWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Невалидный email (нет '@') → 400 + user.email.invalid в Errors.
    /// </summary>
    [Fact]
    public async Task Register_InvalidEmail_Returns400WithEmailInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = "not-an-email",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        payload!.Errors.Should().Contain(e => e.ErrorCode == "user.email.invalid");
    }

    /// <summary>
    /// Слишком короткий пароль → 400 + user.password.too_short.
    /// MinPasswordLength определена в PasswordPolicy.
    /// </summary>
    [Fact]
    public async Task Register_TooShortPassword_Returns400WithPasswordTooShort()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = $"int-{Guid.NewGuid():N}@example.com",
            password = "abc" // короче MinPasswordLength.
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        payload!.Errors.Should().Contain(e => e.ErrorCode == "user.password.too_short");
    }

    /// <summary>
    /// D7.54: слишком длинный пароль (BCrypt-DoS защита).
    /// MaxPasswordLength = 72 байта (BCrypt input limit).
    /// </summary>
    [Fact]
    public async Task Register_TooLongPassword_Returns400WithPasswordTooLong()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = $"int-{Guid.NewGuid():N}@example.com",
            password = new string('a', 1000) // > MaxPasswordLength.
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        payload!.Errors.Should().Contain(e => e.ErrorCode == "user.password.too_long");
    }

    /// <summary>
    /// Повторный register с тем же email → 409 + user.email.already.exists.
    /// Дубликат ловится UniqueConstraintException на ux_users_email.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_Returns409WithEmailAlreadyExists()
    {
        var email = $"int-{Guid.NewGuid():N}@example.com";

        // Первый раз — успех.
        var first = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Password123!"
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Второй раз — конфликт.
        var second = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "OtherPassword123!"
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var payload = await second.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        payload!.ErrorCode.Should().Be("user.email.already.exists");
    }

    /// <summary>
    /// DTO для разбора ApiResponse&lt;object&gt; в случае ошибки.
    /// `Result` — типизирован как `object?`, потому что в API
    /// `ApiResponse&lt;T&gt;.Result` имеет тип T (payload, не bool!).
    /// Для error-кейса payload = null, и попытка парсить его как
    /// bool рушит System.Text.Json.
    /// Валидация-ошибка возвращает ErrorCode = "validation.failed"
    /// и конкретные коды в Errors[].
    /// </summary>
    private sealed class ApiErrorResponse
    {
        public object? Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ValidationErrorDetail> Errors { get; set; } = new();
    }

    private sealed class ValidationErrorDetail
    {
        public string PropertyName { get; set; } = null!;
        public string ErrorCode { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
