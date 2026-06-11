using System.Globalization;
using System.Text;

namespace GdeOni.Mobile.Shared.Routing;

/// <summary>
/// Pure-функции сборки deep-link URL для внешних карт (Yandex / Google /
/// 2ГИС). Не зависит от MAUI/Launcher — тестируется юнит-тестами.
/// Сетевое открытие URL — ответственность ExternalMapsService в mobile-
/// проекте, который зовёт <see cref="Build"/> и передаёт результат в
/// Launcher.OpenAsync.
/// </summary>
public static class ExternalMapsUrlBuilder
{
    /// <summary>
    /// Возвращает URL для выбранного провайдера. origin может быть null —
    /// тогда первая точка из points станет точкой старта.
    /// </summary>
    public static string Build(
        ExternalMapsProvider provider,
        RoutePoint? origin,
        IReadOnlyList<RoutePoint> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (points.Count == 0)
            throw new ArgumentException("At least one point is required.", nameof(points));

        return provider switch
        {
            ExternalMapsProvider.Yandex => BuildYandexUrl(origin, points),
            ExternalMapsProvider.Google => BuildGoogleUrl(origin, points),
            ExternalMapsProvider.DoubleGis => Build2GisUrl(origin, points),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
    }

    // https://yandex.ru/maps/?rtext=lat,lon~lat,lon~...&rtt=auto
    // Яндекс не делает свою оптимизацию порядка — отдаём уже
    // отсортированные через TSP координаты.
    public static string BuildYandexUrl(RoutePoint? origin, IReadOnlyList<RoutePoint> points)
    {
        var rtext = new StringBuilder();
        if (origin is not null)
        {
            AppendLatLon(rtext, origin);
            rtext.Append('~');
        }
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) rtext.Append('~');
            AppendLatLon(rtext, points[i]);
        }
        return $"https://yandex.ru/maps/?rtext={rtext}&rtt=auto";
    }

    // https://www.google.com/maps/dir/?api=1&origin=...&destination=...
    //   &waypoints=lat,lon|lat,lon&travelmode=driving
    // Google требует обязательные origin и destination; промежуточные через
    // waypoints (универсальный URL поддерживает до 9 waypoints).
    public static string BuildGoogleUrl(RoutePoint? origin, IReadOnlyList<RoutePoint> points)
    {
        var sb = new StringBuilder("https://www.google.com/maps/dir/?api=1&travelmode=driving");

        RoutePoint originPoint;
        RoutePoint destinationPoint;
        IReadOnlyList<RoutePoint> waypoints;

        if (origin is not null)
        {
            originPoint = origin;
            destinationPoint = points[^1];
            waypoints = points.Count > 1
                ? points.Take(points.Count - 1).ToList()
                : Array.Empty<RoutePoint>();
        }
        else
        {
            originPoint = points[0];
            destinationPoint = points.Count > 1 ? points[^1] : points[0];
            waypoints = points.Count > 2
                ? points.Skip(1).Take(points.Count - 2).ToList()
                : Array.Empty<RoutePoint>();
        }

        sb.Append("&origin=").Append(FormatLatLon(originPoint));
        sb.Append("&destination=").Append(FormatLatLon(destinationPoint));
        if (waypoints.Count > 0)
        {
            sb.Append("&waypoints=");
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(FormatLatLon(waypoints[i]));
            }
        }
        return sb.ToString();
    }

    // https://2gis.ru/routeSearch/rsType/car/points/lon,lat|lon,lat|...
    // 2ГИС использует ОБРАТНЫЙ порядок координат (lon,lat) — об этом легко
    // забыть и получить точку посреди океана.
    public static string Build2GisUrl(RoutePoint? origin, IReadOnlyList<RoutePoint> points)
    {
        var sb = new StringBuilder("https://2gis.ru/routeSearch/rsType/car/points/");
        bool first = true;
        if (origin is not null)
        {
            AppendLonLat(sb, origin);
            first = false;
        }
        foreach (var p in points)
        {
            if (!first) sb.Append('|');
            AppendLonLat(sb, p);
            first = false;
        }
        return sb.ToString();
    }

    private static string FormatLatLon(RoutePoint p) =>
        string.Format(CultureInfo.InvariantCulture, "{0:F6},{1:F6}", p.Latitude, p.Longitude);

    private static void AppendLatLon(StringBuilder sb, RoutePoint p) =>
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:F6},{1:F6}", p.Latitude, p.Longitude);

    private static void AppendLonLat(StringBuilder sb, RoutePoint p) =>
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:F6},{1:F6}", p.Longitude, p.Latitude);
}
