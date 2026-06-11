using System.Globalization;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Domain.Shared;

namespace GdeOni.Infrastructure.Routing;

/// <summary>
/// Deep-link для 2GIS:
/// https://2gis.ru/directions?from=fromLon,fromLat&to=toLon,toLat&type=auto
/// 2GIS принимает координаты в порядке lon,lat (а не lat,lon).
/// type: auto / walking / pt (public transport) / bicycle.
/// </summary>
public sealed class TwoGisRouteLinkProvider : IRouteLinkProvider
{
    public string ProviderKey => "2gis";

    public string BuildLink(double fromLat, double fromLon, double toLat, double toLon, RoutingMode mode)
    {
        var type = mode switch
        {
            RoutingMode.Pedestrian => "walking",
            RoutingMode.MassTransit => "pt",
            RoutingMode.Bicycle => "bicycle",
            _ => "auto"
        };

        var inv = CultureInfo.InvariantCulture;
        return
            $"https://2gis.ru/directions?from={fromLon.ToString(inv)},{fromLat.ToString(inv)}" +
            $"&to={toLon.ToString(inv)},{toLat.ToString(inv)}&type={type}";
    }
}
