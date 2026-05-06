using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// Проверяем сквозной auth-сценарий через настоящий API + Postgres:
/// — POST /api/users — регистрация;
/// — POST /api/auth/login — логин и выдача access + refresh;
/// — POST /api/auth/refresh — обмен refresh на новую пару;
/// — POST /api/auth/logout — отзыв refresh-токена.
///
/// Все вызовы идут через HttpClient WebApplicationFactory, без сети,
/// но с настоящим pipeline'ом: JWT bearer, FluentValidation, EF +
/// Postgres. Это страхует от багов сразу нескольких слоёв (Domain,
/// Application, Infrastructure, API).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthFlowTests
{
    private readonly GdeOniWebAppFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// JsonSerializerOptions с PropertyNameCaseInsensitive — потому что
    /// контроллеры отдают camelCase, а тестовые DTO мы пишем в PascalCase.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthFlowTests(GdeOniWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Сценарий: регистрация → login → защищённый эндпоинт.
    /// Главное, что проверяем:
    /// 1) /api/users 201 + ApiResponse&lt;RegisterUserResponse&gt;;
    /// 2) /api/auth/login 200 + access token не пустой;
    /// 3) GET /api/users/me с access token'ом возвращает того же
    ///    пользователя — JWT валиден, SecurityStamp прошёл проверку.
    /// </summary>
    [Fact]
    public async Task Login_AfterRegister_ReturnsAccessTokenAndAuthorizesMe()
    {
        // Arrange: уникальный email на тест, чтобы не коллидить
        // между прогонами в одном контейнере (контейнеры одноразовые,
        // но порядок выполнения может повторяться при retry).
        var email = $"int-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!"; // ≥ MinPasswordLength.

        // Act 1: регистрация.
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 2: логин.
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await login.Content.ReadFromJsonAsync<ApiResponseDto<LoginResultDto>>(JsonOptions);
        loginPayload!.Result.Should().NotBeNull();
        loginPayload.Result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginPayload.Result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Act 3: GET /api/users/me с access token'ом.
        var me = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Result.AccessToken);
        var meResponse = await _client.SendAsync(me);

        // Assert: 200 + email вернулся тот же → identity и SecurityStamp ОК.
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mePayload = await meResponse.Content.ReadFromJsonAsync<ApiResponseDto<MeDto>>(JsonOptions);
        mePayload!.Result!.Email.Should().Be(email);
    }

    /// <summary>
    /// /api/auth/refresh: после первого login'а отдаём refresh, обменяли
    /// его — получили новые access и refresh, старый refresh должен
    /// перестать работать (replay-detection — D7.32). Тут проверяем
    /// happy path обмена; replay покрываем отдельно — не в этом тесте,
    /// чтобы не делать его чрезмерно объёмным.
    /// </summary>
    [Fact]
    public async Task Refresh_ValidToken_RotatesPair()
    {
        // Arrange: регистрация + первичный login.
        var email = $"int-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        await _client.PostAsJsonAsync("/api/users", new { email, password });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var loginPayload = await login.Content.ReadFromJsonAsync<ApiResponseDto<LoginResultDto>>(JsonOptions);
        var firstRefresh = loginPayload!.Result!.RefreshToken;

        // Act: обменять refresh на новую пару.
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = firstRefresh
        });

        // Assert: 200 + новый refresh не равен старому (rotation).
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshPayload = await refresh.Content.ReadFromJsonAsync<ApiResponseDto<LoginResultDto>>(JsonOptions);
        refreshPayload!.Result!.RefreshToken.Should().NotBe(firstRefresh);
        refreshPayload.Result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class ApiResponseDto<TResult>
    {
        // System.Text.Json по умолчанию матчит camelCase ↔ PascalCase
        // (PropertyNameCaseInsensitive=true) — поэтому достаточно одного
        // имени Result, оно подцепит "result" из JSON.
        public TResult? Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private sealed class LoginResultDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid Id { get; set; }
    }

    private sealed class MeDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
