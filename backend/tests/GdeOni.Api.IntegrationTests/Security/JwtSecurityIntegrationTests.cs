using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.API.Security;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.Api.IntegrationTests.Security;

/// <summary>
/// D9.5.4 JWT/Security: запрос без header → 401, expired → 401,
/// подделанная подпись → 401, после ChangePassword старый JWT → 401,
/// admin endpoint без admin role → 403.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class JwtSecurityIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;
    private readonly HttpClient _anonymous;

    public JwtSecurityIntegrationTests(GdeOniWebAppFactory factory)
    {
        _factory = factory;
        _anonymous = factory.CreateClient();
    }

    /// <summary>
    /// Запрос к /me без Authorization header → 401.
    /// </summary>
    [Fact]
    public async Task NoAuthHeader_Returns401()
    {
        var response = await _anonymous.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Истёкший JWT → 401. Делаем токен с exp в прошлом и подписываем
    /// тем же ключом, что и фабрика.
    /// </summary>
    [Fact]
    public async Task ExpiredJwt_Returns401()
    {
        var expiredToken = BuildJwt(
            userId: Guid.NewGuid(),
            issuer: "GdeOni.Tests",
            audience: "GdeOni.Tests.Client",
            secretKey: "test-secret-key-with-at-least-32-bytes!!",
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-5));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _anonymous.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// JWT, подписанный другим ключом (подделка) → 401.
    /// </summary>
    [Fact]
    public async Task ForgedSignature_Returns401()
    {
        var forged = BuildJwt(
            userId: Guid.NewGuid(),
            issuer: "GdeOni.Tests",
            audience: "GdeOni.Tests.Client",
            secretKey: "completely-different-key-also-32-bytes!",
            expiresAtUtc: DateTime.UtcNow.AddMinutes(15));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await _anonymous.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// После ChangePassword (через PUT /api/users/{id}/password) SecurityStamp
    /// у юзера ротируется. Старый access-token с прошлым stamp → 401 на
    /// следующем GET /me. SecurityStampCacheTtlSeconds = 30 в тестах,
    /// но кеш переписывается при выдаче нового токена write-through —
    /// поэтому старый stamp инвалидируется немедленно.
    /// </summary>
    [Fact]
    public async Task AfterChangePassword_OldAccessToken_Returns401()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var oldAccess = user.AccessToken;

        // Меняем пароль — SecurityStamp ротируется и login-flow выдаёт новый
        // токен (это пишет новый stamp в кеш).
        var change = await user.Client.PutAsJsonAsync(
            $"/api/users/{user.Id}/password",
            new
            {
                currentPassword = user.Password,
                newPassword = "Password123!Updated"
            });
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        // Логинимся ещё раз — это перетирает кеш SecurityStamp на новый
        // (write-through из JwtProvider).
        var fresh = _factory.CreateClient();
        var loginResp = await fresh.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = "Password123!Updated"
        });
        loginResp.EnsureSuccessStatusCode();

        // Теперь старый токен с прошлым stamp → 401.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oldAccess);
        var response = await _anonymous.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// RegularUser идёт на admin endpoint → 403 (Authorize Roles enforce'ится).
    /// </summary>
    [Fact]
    public async Task RegularUser_OnAdminEndpoint_Returns403()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string BuildJwt(
        Guid userId,
        string issuer,
        string audience,
        string secretKey,
        DateTime expiresAtUtc)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtClaimNames.SecurityStamp, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "fake@example.com"),
            new Claim(ClaimTypes.Name, "fake"),
            new Claim(ClaimTypes.Role, "RegularUser")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
