using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// Тесты JWT-валидации на уровне HTTP pipeline'а:
/// — без Authorization → 401;
/// — невалидный bearer (произвольная строка) → 401;
/// — bearer с правильной подписью но неправильным issuer/audience
///   → выходит за рамки этого теста, покрывается в JwtBearerEvents.
///
/// Тесты требуют Docker (контейнерная БД для seed'а).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class JwtSecurityTests
{
    private readonly HttpClient _client;

    public JwtSecurityTests(GdeOniWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// GET /api/users/me без Authorization header → 401.
    /// Стандартное поведение [Authorize] middleware.
    /// </summary>
    [Fact]
    public async Task GetMe_WithoutAuthorizationHeader_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// GET /api/users/me с подделанным Bearer (не JWT-формата) → 401.
    /// JwtBearerHandler парсит заголовок и не находит валидную подпись.
    /// </summary>
    [Fact]
    public async Task GetMe_WithInvalidBearer_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "fake.jwt.token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// GET /api/users (admin-only) с обычным RegularUser → 403.
    /// Проверяем, что [Authorize(Roles = "SuperAdmin,Admin")] работает
    /// в pipeline'е (а не только в логике use case'а).
    /// </summary>
    [Fact]
    public async Task GetAllUsers_AsRegularUser_Returns403()
    {
        // Регистрируем обычного юзера и логинимся.
        var email = $"int-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Password123!",
            privacyPolicyAccepted = true,
            termsAccepted = true
        });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginPayload>();

        // Bearer от обычного юзера, GET admin-эндпоинта.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Result!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class LoginPayload
    {
        public LoginResultDto? Result { get; set; }
    }

    private sealed class LoginResultDto
    {
        public string AccessToken { get; set; } = null!;
    }
}
