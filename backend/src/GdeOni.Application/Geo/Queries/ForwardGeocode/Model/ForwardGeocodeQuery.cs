namespace GdeOni.Application.Geo.Queries.ForwardGeocode.Model;

/// <summary>Текст адреса (город / кладбище, страна) → координаты.</summary>
public sealed record ForwardGeocodeQuery(string Query);

/// <summary>Ответ клиенту: координаты найденного места.</summary>
public sealed record ForwardGeocodeResponse(
    double Latitude,
    double Longitude,
    string? DisplayName);
