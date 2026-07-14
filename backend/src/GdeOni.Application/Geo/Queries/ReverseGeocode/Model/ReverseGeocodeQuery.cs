namespace GdeOni.Application.Geo.Queries.ReverseGeocode.Model;

/// <summary>D41. Координаты → адрес.</summary>
public sealed record ReverseGeocodeQuery(double Latitude, double Longitude);

/// <summary>
/// Ответ клиенту. Все поля опциональны: посреди леса города не будет.
/// </summary>
public sealed record ReverseGeocodeResponse(
    string? Country,
    string? Region,
    string? City);
