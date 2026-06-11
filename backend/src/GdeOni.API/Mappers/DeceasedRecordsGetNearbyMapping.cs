using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;

namespace GdeOni.API.Mappers;

public static class DeceasedRecordsGetNearbyMapping
{
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
