using System.Net;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Health;

/// <summary>
/// D21. Тесты <c>/health</c> endpoint'а.
///
/// Testcontainers поднимает PostgreSQL + MinIO, поэтому в integration-
/// контексте оба health-check'а должны быть зелёными → /health = 200
/// + status: "Healthy". Тестирование негативного кейса (Postgres down →
/// 503) тут не делаем — оно требует остановки контейнера прямо в тесте,
/// что усложняет setup. Sentry-капчу негативного кейса проверяем в проде.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HealthEndpointTests(GdeOniWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_Anonymous_Returns200WithHealthyStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_ReturnsPostgresqlAndMinioChecks()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var names = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();
        names.Should().Contain("postgresql");
        names.Should().Contain("minio");
    }

    [Fact]
    public async Task GetHealth_NotGatedBySubscriptionPolicy()
    {
        // Балансировщик / liveness-probe ходит на /health без JWT.
        // Если бы DefaultPolicy = RequireActiveSubscription применялся,
        // ответ был бы 401, а не 200.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
