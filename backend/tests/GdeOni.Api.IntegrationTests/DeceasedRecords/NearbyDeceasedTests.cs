using System.Net;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.DeceasedRecords;

/// <summary>
/// E21. GET /api/deceased-records/nearby — поиск умерших в радиусе
/// от точки. Проверяем диапазоны валидатора, фильтрацию по радиусу
/// и сортировку по дистанции.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NearbyDeceasedTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NearbyDeceasedTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// Аноним → 401.
    /// </summary>
    [Fact]
    public async Task GetNearby_Anonymous_Returns401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync(
            "/api/deceased-records/nearby?latitude=55.7558&longitude=37.6173&radiusMeters=100");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Авторизованный без результатов в радиусе → 200 + empty.
    /// Запрашиваем точку посреди Тихого океана — там точно ничего
    /// нет (даже если в БД есть другие записи).
    /// </summary>
    [Fact]
    public async Task GetNearby_NoResults_Returns200WithEmpty()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=0.0&longitude=-150.0&radiusMeters=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    /// <summary>
    /// Карточка в 30 метрах от точки поиска при радиусе 100 → найдена;
    /// другая в 5 км — НЕ найдена. Проверяем что поиск действительно
    /// фильтрует по радиусу, а не возвращает всю базу.
    /// </summary>
    [Fact]
    public async Task GetNearby_FiltersByRadius_FindsClose_SkipsFar()
    {
        var alice = await _factory.RegisterAndLoginAsync();

        // База: точка поиска (55.7558, 37.6173) — центр Москвы.
        // 1° lat ≈ 111.32 км, поэтому 0.00027° ≈ 30 м к северу.
        var nearMarker = $"NearMe{Guid.NewGuid():N}";
        var nearId = await TestSeed.CreateAtGraveAtCoordsAsync(
            alice.Client, latitude: 55.7558 + 0.00027, longitude: 37.6173,
            lastName: nearMarker);

        // 0.05° ≈ 5.5 км — далеко за пределами 100м радиуса.
        var farMarker = $"FarAway{Guid.NewGuid():N}";
        var farId = await TestSeed.CreateAtGraveAtCoordsAsync(
            alice.Client, latitude: 55.7558 + 0.05, longitude: 37.6173,
            lastName: farMarker);

        var response = await alice.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=55.7558&longitude=37.6173&radiusMeters=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(nearId.ToString());
        body.Should().NotContain(farId.ToString());
    }

    /// <summary>
    /// Результаты отсортированы по DistanceMeters по возрастанию.
    /// Создаём две карточки на 30м и 80м, проверяем порядок.
    /// </summary>
    [Fact]
    public async Task GetNearby_SortsByDistanceAscending()
    {
        var alice = await _factory.RegisterAndLoginAsync();

        var closerMarker = $"Closer{Guid.NewGuid():N}";
        var fartherMarker = $"Farther{Guid.NewGuid():N}";

        // ~30 м к северу.
        var closerId = await TestSeed.CreateAtGraveAtCoordsAsync(
            alice.Client, 55.7558 + 0.00027, 37.6173, closerMarker);
        // ~80 м к северу.
        var fartherId = await TestSeed.CreateAtGraveAtCoordsAsync(
            alice.Client, 55.7558 + 0.00072, 37.6173, fartherMarker);

        var response = await alice.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=55.7558&longitude=37.6173&radiusMeters=200");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("result").GetProperty("items");

        // Берём наши две записи по lastName-маркерам.
        var closerIdx = -1;
        var fartherIdx = -1;
        for (int i = 0; i < items.GetArrayLength(); i++)
        {
            var name = items[i].GetProperty("fullName").GetString() ?? string.Empty;
            if (name.Contains(closerMarker)) closerIdx = i;
            if (name.Contains(fartherMarker)) fartherIdx = i;
        }

        closerIdx.Should().BeGreaterThanOrEqualTo(0);
        fartherIdx.Should().BeGreaterThanOrEqualTo(0);
        closerIdx.Should().BeLessThan(fartherIdx,
            "более близкая карточка должна быть выше по списку");

        // И DistanceMeters у ближней меньше чем у дальней.
        var closerDist = items[closerIdx].GetProperty("distanceMeters").GetInt32();
        var fartherDist = items[fartherIdx].GetProperty("distanceMeters").GetInt32();
        closerDist.Should().BeLessThan(fartherDist);
    }

    /// <summary>
    /// radiusMeters=5 (ниже минимума 10) → 400 Validation.
    /// </summary>
    [Fact]
    public async Task GetNearby_RadiusBelowMinimum_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var response = await user.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=55.0&longitude=37.0&radiusMeters=5");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// radiusMeters=10000 (выше максимума 5000) → 400 Validation.
    /// Защита от "поиск по всей планете" — выше 5 км это уже не "рядом".
    /// </summary>
    [Fact]
    public async Task GetNearby_RadiusAboveMaximum_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var response = await user.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=55.0&longitude=37.0&radiusMeters=10000");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// lat=91 → 400 Validation (BurialLocation.LatitudeInvalid).
    /// </summary>
    [Fact]
    public async Task GetNearby_LatitudeOutOfRange_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var response = await user.Client.GetAsync(
            "/api/deceased-records/nearby?latitude=91&longitude=37.0&radiusMeters=100");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
