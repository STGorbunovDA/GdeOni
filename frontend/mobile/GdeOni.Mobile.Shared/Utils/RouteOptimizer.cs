using GdeOni.Mobile.Shared.Routing;

namespace GdeOni.Mobile.Shared.Utils;

/// <summary>
/// TSP nearest-neighbor для оптимизации порядка обхода точек. На реальном
/// масштабе (2–10 могил за день) даёт результат, неотличимый от точного
/// оптимума, при O(N²).
///
/// Не зависит от Microsoft.Maui.Devices.Sensors — собственный haversine,
/// чтобы Shared-сборка спокойно жила в test-проекте без MAUI runtime.
/// </summary>
public static class RouteOptimizer
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Возвращает упорядоченные точки по nearest-neighbor от origin
    /// (если origin null — стартуем с первой точки из points). Не мутирует
    /// входной список.
    /// </summary>
    public static List<RoutePoint> OptimizeOrder(
        RoutePoint? origin,
        IReadOnlyList<RoutePoint> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (points.Count <= 1) return new List<RoutePoint>(points);

        var remaining = new List<RoutePoint>(points);
        var ordered = new List<RoutePoint>(points.Count);

        RoutePoint current;
        if (origin is not null)
        {
            current = origin;
        }
        else
        {
            current = remaining[0];
            ordered.Add(current);
            remaining.RemoveAt(0);
        }

        while (remaining.Count > 0)
        {
            var bestIdx = 0;
            var bestDist = double.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                var d = HaversineKm(current, remaining[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }
            current = remaining[bestIdx];
            ordered.Add(current);
            remaining.RemoveAt(bestIdx);
        }

        return ordered;
    }

    /// <summary>
    /// Расстояние между двумя точками по большому кругу (километры).
    /// Стандартная haversine-формула.
    /// </summary>
    public static double HaversineKm(RoutePoint a, RoutePoint b)
    {
        var dLat = ToRadians(b.Latitude - a.Latitude);
        var dLon = ToRadians(b.Longitude - a.Longitude);

        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
