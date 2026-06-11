using System.Net;
using System.Net.Http.Json;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// D9.5.4: rate-limiting на /api/auth/login. PermitLimit=3 в фабрике,
/// поэтому после 3 запросов с одного IP следующие → 429 + Retry-After.
/// Используем IClassFixture, чтобы не делить состояние с общей коллекцией.
/// </summary>
public sealed class RateLimitTests : IClassFixture<RateLimitedWebAppFactory>
{
    private readonly HttpClient _client;

    public RateLimitTests(RateLimitedWebAppFactory factory) => _client = factory.CreateClient();

    /// <summary>
    /// 4-й login-запрос с того же IP в пределах окна → 429.
    /// PermitLimit=3, sliding-window 60 минут, поэтому permit'ы
    /// не успевают восстановиться между вызовами.
    /// </summary>
    [Fact]
    public async Task Login_AfterPermitLimit_Returns429()
    {
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = $"missing-{Guid.NewGuid():N}@example.com",
                password = "Password123!"
            });
            statuses.Add(response.StatusCode);
        }

        // Первые 3 — 401 (несуществующий пользователь, но через rate-limit прошли).
        // С 4-го — 429.
        statuses.Take(3).Should().OnlyContain(s => s == HttpStatusCode.Unauthorized);
        statuses.Skip(3).Should().Contain(HttpStatusCode.TooManyRequests);
    }
}
