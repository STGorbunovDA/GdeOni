using FluentAssertions;
using GdeOni.Mobile.Shared.Routing;
using Xunit;

namespace GdeOni.Mobile.Tests.Routing;

public class ExternalMapsUrlBuilderTests
{
    private static readonly RoutePoint Origin = new(55.755826, 37.617300); // Москва
    private static readonly RoutePoint A = new(59.939099, 30.315877);       // Питер
    private static readonly RoutePoint B = new(56.838011, 60.597474);       // Екатеринбург

    // ───────────────────────────── Yandex ─────────────────────────────

    [Fact]
    public void Yandex_with_origin_and_two_points_builds_rtext_in_order()
    {
        var url = ExternalMapsUrlBuilder.BuildYandexUrl(Origin, new[] { A, B });

        url.Should().Be(
            "https://yandex.ru/maps/?rtext=55.755826,37.617300~59.939099,30.315877~56.838011,60.597474&rtt=auto");
    }

    [Fact]
    public void Yandex_without_origin_omits_starting_segment()
    {
        var url = ExternalMapsUrlBuilder.BuildYandexUrl(null, new[] { A, B });

        url.Should().Be(
            "https://yandex.ru/maps/?rtext=59.939099,30.315877~56.838011,60.597474&rtt=auto");
    }

    [Fact]
    public void Yandex_single_point_with_origin()
    {
        var url = ExternalMapsUrlBuilder.BuildYandexUrl(Origin, new[] { A });

        url.Should().Be(
            "https://yandex.ru/maps/?rtext=55.755826,37.617300~59.939099,30.315877&rtt=auto");
    }

    // ───────────────────────────── Google ─────────────────────────────

    [Fact]
    public void Google_with_origin_places_last_point_as_destination_others_as_waypoints()
    {
        var url = ExternalMapsUrlBuilder.BuildGoogleUrl(Origin, new[] { A, B });

        url.Should().Be(
            "https://www.google.com/maps/dir/?api=1&travelmode=driving"
            + "&origin=55.755826,37.617300"
            + "&destination=56.838011,60.597474"
            + "&waypoints=59.939099,30.315877");
    }

    [Fact]
    public void Google_without_origin_uses_first_point_as_origin_last_as_destination()
    {
        var url = ExternalMapsUrlBuilder.BuildGoogleUrl(null, new[] { A, B });

        url.Should().Be(
            "https://www.google.com/maps/dir/?api=1&travelmode=driving"
            + "&origin=59.939099,30.315877"
            + "&destination=56.838011,60.597474");
    }

    [Fact]
    public void Google_single_point_with_origin_has_no_waypoints()
    {
        var url = ExternalMapsUrlBuilder.BuildGoogleUrl(Origin, new[] { A });

        url.Should().Be(
            "https://www.google.com/maps/dir/?api=1&travelmode=driving"
            + "&origin=55.755826,37.617300"
            + "&destination=59.939099,30.315877");
    }

    [Fact]
    public void Google_three_points_without_origin_middle_becomes_waypoint()
    {
        var url = ExternalMapsUrlBuilder.BuildGoogleUrl(null, new[] { A, Origin, B });

        url.Should().Be(
            "https://www.google.com/maps/dir/?api=1&travelmode=driving"
            + "&origin=59.939099,30.315877"
            + "&destination=56.838011,60.597474"
            + "&waypoints=55.755826,37.617300");
    }

    // ───────────────────────────── 2ГИС ─────────────────────────────

    [Fact]
    public void TwoGis_uses_reverse_lon_lat_order()
    {
        // CRITICAL: 2ГИС использует обратный порядок координат (lon,lat).
        // Если кто-то когда-нибудь "пофиксит" это на (lat,lon) — точка
        // окажется посреди Атлантики.
        var url = ExternalMapsUrlBuilder.Build2GisUrl(Origin, new[] { A });

        url.Should().Be(
            "https://2gis.ru/routeSearch/rsType/car/points/"
            + "37.617300,55.755826|30.315877,59.939099");
    }

    [Fact]
    public void TwoGis_without_origin_starts_with_first_point()
    {
        var url = ExternalMapsUrlBuilder.Build2GisUrl(null, new[] { A, B });

        url.Should().Be(
            "https://2gis.ru/routeSearch/rsType/car/points/"
            + "30.315877,59.939099|60.597474,56.838011");
    }

    [Fact]
    public void TwoGis_multiple_points_separated_by_pipe()
    {
        var url = ExternalMapsUrlBuilder.Build2GisUrl(Origin, new[] { A, B });

        url.Should().Be(
            "https://2gis.ru/routeSearch/rsType/car/points/"
            + "37.617300,55.755826|30.315877,59.939099|60.597474,56.838011");
    }

    // ───────────────────── Common: культура / форматирование ─────────────────────

    [Fact]
    public void Negative_and_fractional_coordinates_format_with_six_decimals_and_dot()
    {
        // Тест защищает от поломки при ru-RU локали (могла бы появиться
        // запятая вместо точки) и от потери точности при ToString().
        var url = ExternalMapsUrlBuilder.BuildYandexUrl(
            new RoutePoint(-12.345678, -45.678901),
            new[] { new RoutePoint(0.0, 0.0) });

        url.Should().Contain("rtext=-12.345678,-45.678901~0.000000,0.000000");
    }

    // ───────────────────── Build(provider, ...) dispatcher ─────────────────────

    [Theory]
    [InlineData(ExternalMapsProvider.Yandex, "yandex.ru/maps")]
    [InlineData(ExternalMapsProvider.Google, "google.com/maps/dir")]
    [InlineData(ExternalMapsProvider.DoubleGis, "2gis.ru/routeSearch")]
    public void Build_dispatcher_returns_provider_specific_url(
        ExternalMapsProvider provider, string expectedHostFragment)
    {
        var url = ExternalMapsUrlBuilder.Build(provider, Origin, new[] { A });
        url.Should().Contain(expectedHostFragment);
    }

    [Fact]
    public void Build_throws_when_points_empty()
    {
        var act = () => ExternalMapsUrlBuilder.Build(
            ExternalMapsProvider.Yandex, Origin, Array.Empty<RoutePoint>());
        act.Should().Throw<ArgumentException>();
    }
}
