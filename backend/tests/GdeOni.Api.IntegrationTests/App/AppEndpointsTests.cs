using System.Net;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.App;

/// <summary>
/// D17. /api/app/version и /api/app/features.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AppEndpointsTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppEndpointsTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// GET /api/app/version без auth → 200 + дефолтные значения
    /// (фабрика не подмешивает AppVersion в конфиг → дефолты "1.0.0"
    /// из AppVersionOptions). AllowAnonymous важен: старый клиент с
    /// протухшим токеном должен узнать о принудительном обновлении.
    /// </summary>
    [Fact]
    public async Task GetVersion_Anonymous_Returns200WithDefaults()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/app/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("minSupportedVersion").GetString().Should().Be("1.0.0");
        result.GetProperty("latestVersion").GetString().Should().Be("1.0.0");
    }

    /// <summary>
    /// GET /api/app/features без auth → 401. Защита от утечки
    /// операционных флагов анонимам.
    /// </summary>
    [Fact]
    public async Task GetFeatures_Anonymous_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/app/features");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// GET /api/app/features авторизованным → 200 с дефолтами
    /// FeatureFlagsOptions (SubscriptionEnabled=false,
    /// GracePeriodDaysAfterExpiry=0).
    /// </summary>
    [Fact]
    public async Task GetFeatures_Authenticated_Returns200WithDefaults()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/app/features");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("subscriptionEnabled").GetBoolean().Should().BeFalse();
        result.GetProperty("gracePeriodDaysAfterExpiry").GetInt32().Should().Be(0);
        // Геолокация: секция в тестах не задана → дефолты 60 c и 0.5 м.
        result.GetProperty("geoAcquireWindowSeconds").GetInt32().Should().Be(60);
        result.GetProperty("geoTargetAccuracyMeters").GetDouble().Should().Be(0.5);
    }
}
