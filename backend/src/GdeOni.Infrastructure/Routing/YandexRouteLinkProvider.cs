using System.Globalization;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Domain.Shared;

namespace GdeOni.Infrastructure.Routing;

/// <summary>
/// Deep-link для Яндекс.Карт. Формат:
/// https://yandex.ru/maps/?rtext=fromLat,fromLon~toLat,toLon&rtt=auto
/// rtt: auto / pd (pedestrian) / mt (mass transit) / bc (bicycle).
/// </summary>
public sealed class YandexRouteLinkProvider : IRouteLinkProvider
{
    public string ProviderKey => "yandex";

    public string BuildLink(double fromLat, double fromLon, double toLat, double toLon, RoutingMode mode)
    {
        var rtt = mode switch
        {
            RoutingMode.Pedestrian => "pd",
            RoutingMode.MassTransit => "mt",
            RoutingMode.Bicycle => "bc",
            _ => "auto"
        };

        var inv = CultureInfo.InvariantCulture;
        return
            $"https://yandex.ru/maps/?rtext={fromLat.ToString(inv)},{fromLon.ToString(inv)}" +
            $"~{toLat.ToString(inv)},{toLon.ToString(inv)}&rtt={rtt}";
    }
}
