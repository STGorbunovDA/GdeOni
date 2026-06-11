namespace GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;

/// <summary>
/// E21. Поиск умерших в радиусе от точки (lat, lon). RadiusMeters
/// валидируется в диапазоне [<see cref="GetNearbyDeceasedQueryValidator.MinRadiusMeters"/>,
/// <see cref="GetNearbyDeceasedQueryValidator.MaxRadiusMeters"/>] — 5 км это
/// уже не "рядом", а на 10 м меньше — GPS-точность мобильного устройства.
/// </summary>
public sealed record GetNearbyDeceasedQuery(
    double Latitude,
    double Longitude,
    double RadiusMeters,
    int Page,
    int PageSize);
