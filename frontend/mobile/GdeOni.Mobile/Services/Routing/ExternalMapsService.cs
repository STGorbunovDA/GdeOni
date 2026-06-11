using GdeOni.Mobile.Shared.Routing;

namespace GdeOni.Mobile.Services.Routing;

// Re-export shared-типов под старым namespace'ом — чтобы остальной mobile-
// код не пришлось переписывать после рефакторинга в Shared. Тестируется
// логика в GdeOni.Mobile.Shared.Routing.ExternalMapsUrlBuilder.
public sealed record RoutePoint(double Latitude, double Longitude)
{
    internal Shared.Routing.RoutePoint ToShared() => new(Latitude, Longitude);
}

public enum ExternalMapsProvider
{
    Yandex = Shared.Routing.ExternalMapsProvider.Yandex,
    Google = Shared.Routing.ExternalMapsProvider.Google,
    DoubleGis = Shared.Routing.ExternalMapsProvider.DoubleGis
}

public interface IExternalMapsService
{
    /// <summary>
    /// Открывает внешнее приложение карт (или web-версию, если приложение
    /// не установлено) с готовым маршрутом через все waypoints в заданном
    /// порядке. origin может быть null — тогда первая точка из points
    /// станет точкой старта.
    /// </summary>
    Task<bool> OpenRouteAsync(
        ExternalMapsProvider provider,
        RoutePoint? origin,
        IReadOnlyList<RoutePoint> points,
        CancellationToken cancellationToken = default);
}

public sealed class ExternalMapsService : IExternalMapsService
{
    public async Task<bool> OpenRouteAsync(
        ExternalMapsProvider provider,
        RoutePoint? origin,
        IReadOnlyList<RoutePoint> points,
        CancellationToken cancellationToken = default)
    {
        if (points.Count == 0)
            return false;

        var sharedProvider = (Shared.Routing.ExternalMapsProvider)provider;
        var sharedOrigin = origin?.ToShared();
        var sharedPoints = points.Select(p => p.ToShared()).ToList();

        var url = ExternalMapsUrlBuilder.Build(sharedProvider, sharedOrigin, sharedPoints);

        try
        {
            return await Launcher.OpenAsync(new Uri(url));
        }
        catch
        {
            return false;
        }
    }
}
