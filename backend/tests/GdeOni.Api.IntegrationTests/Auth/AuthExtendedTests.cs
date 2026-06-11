using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// D9.5.4 Auth-сценарии помимо happy-path: невалидный логин, replay
/// detection, идемпотентность logout. Существующий AuthFlowTests
/// покрыл register/login/refresh happy path; эти тесты — про edge cases.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthExtendedTests
{
    private readonly GdeOniWebAppFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthExtendedTests(GdeOniWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Login с неверным паролем → 401 + user.invalid.credentials.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_Returns401WithInvalidCredentials()
    {
        var (email, _, _, _) = await _factory.RegisterAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(JsonOptions);
        payload!.ErrorCode.Should().Be("user.invalid.credentials");
    }

    /// <summary>
    /// Login с несуществующим email → 401 + те же user.invalid.credentials.
    /// Это намеренно: login не раскрывает существование пользователя.
    /// </summary>
    [Fact]
    public async Task Login_UnknownEmail_Returns401WithSameCredentialsCode()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(JsonOptions);
        payload!.ErrorCode.Should().Be("user.invalid.credentials");
    }

    /// <summary>
    /// Refresh: replay уже использованного refresh-токена → 401
    /// + refresh_token.replay_detected. После этого все RT
    /// пользователя отозваны — повторный refresh свежим токеном
    /// тоже падает.
    /// </summary>
    [Fact]
    public async Task Refresh_Replay_Returns401AndRevokesAll()
    {
        var user = await _factory.RegisterAndLoginAsync();

        // Первый refresh — ОК. Старый RT теперь revoked, новый — свежий.
        var firstRefresh = await user.Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = user.RefreshToken
        });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPayload = await firstRefresh.Content.ReadFromJsonAsync<ApiResultDto<TokenPair>>(JsonOptions);
        var newRefresh = firstPayload!.Result!.RefreshToken;

        // Replay старого refresh → 401 + replay_detected. Это триггерит
        // RevokeAllForUser — все активные RT (включая newRefresh) теперь
        // revoked.
        var replay = await user.Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = user.RefreshToken
        });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var replayPayload = await replay.Content.ReadFromJsonAsync<ApiErrorPayload>(JsonOptions);
        replayPayload!.ErrorCode.Should().Be("refresh_token.replay_detected");

        // Свежий newRefresh тоже теперь не работает — revoked в рамках replay-detection.
        var afterReplay = await user.Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = newRefresh
        });
        afterReplay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Logout с несуществующим refresh-токеном (но валидным auth) → 204.
    /// LogoutUseCase идемпотентен (D7.40): чужой / несуществующий /
    /// уже-revoked токен возвращает Success без Save.
    /// </summary>
    [Fact]
    public async Task Logout_UnknownRefreshToken_Returns204()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = $"non-existing-token-{Guid.NewGuid():N}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Logout без auth-header → 401. Этот endpoint требует [Authorize].
    /// </summary>
    [Fact]
    public async Task Logout_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = "any-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class ApiErrorPayload
    {
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private sealed class ApiResultDto<T>
    {
        public T? Result { get; set; }
    }

    private sealed class TokenPair
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
