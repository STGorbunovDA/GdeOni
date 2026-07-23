using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Query маппинг для запроса "кто рядом" (поиск карточек
/// по радиусу от GPS-точки).
/// </summary>
public static class DeceasedRecordsGetNearbyMapping
{
    /// <summary>Маппит DTO поиска "кто рядом" в запрос use case.</summary>
    public static GetNearbyDeceasedQuery ToQuery(this GetNearbyDeceasedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetNearbyDeceasedQuery(
            request.Latitude,
            request.Longitude,
            request.RadiusMeters,
            request.Page,
            request.PageSize);
    }
}
