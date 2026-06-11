using System.Globalization;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Domain.Shared;

namespace GdeOni.Infrastructure.Routing;

/// <summary>
/// Deep-link для Google Maps (Universal URL API):
/// https://www.google.com/maps/dir/?api=1&origin=fromLat,fromLon&destination=toLat,toLon&travelmode=driving
/// travelmode: driving / walking / transit / bicycling.
/// </summary>
public sealed class GoogleMapsRouteLinkProvider : IRouteLinkProvider
{
    public string ProviderKey => "google";

    public string BuildLink(double fromLat, double fromLon, double toLat, double toLon, RoutingMode mode)
    {
        var travelMode = mode switch
        {
            RoutingMode.Pedestrian => "walking",
            RoutingMode.MassTransit => "transit",
            RoutingMode.Bicycle => "bicycling",
            _ => "driving"
        };

        var inv = CultureInfo.InvariantCulture;
        return
            $"https://www.google.com/maps/dir/?api=1" +
            $"&origin={fromLat.ToString(inv)},{fromLon.ToString(inv)}" +
            $"&destination={toLat.ToString(inv)},{toLon.ToString(inv)}" +
            $"&travelmode={travelMode}";
    }
}
