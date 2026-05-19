namespace GdeOni.Mobile.Services.Api.Models;

public sealed record RouteResponse(
    Guid DeceasedId,
    GraveLocation GraveLocation,
    FromCoordinates From,
    string Mode,
    IReadOnlyList<RouteLink> Links);

public sealed record GraveLocation(
    double Latitude,
    double Longitude,
    double? AccuracyMeters);

public sealed record FromCoordinates(
    double Latitude,
    double Longitude);

public sealed record RouteLink(
    string Provider,
    string Url);

/// <summary>
/// Имена RoutingMode на backend (D11.1.1 — enum'ы сериализуются строками).
/// </summary>
public static class RoutingModes
{
    public const string Auto = "Auto";
    public const string Pedestrian = "Pedestrian";
    public const string MassTransit = "MassTransit";
    public const string Bicycle = "Bicycle";
}

/// <summary>
/// ProviderKey'и из backend IRouteLinkProvider.
/// </summary>
public static class RouteProviders
{
    public const string Yandex = "yandex";
    public const string Google = "google";
    public const string TwoGis = "2gis";

    public static string Display(string providerKey) => providerKey switch
    {
        Yandex => "Яндекс.Карты",
        Google => "Google Maps",
        TwoGis => "2ГИС",
        _ => providerKey
    };
}
