using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Query маппинг для листинга карточек умерших с
/// пагинацией и фильтрами.
/// </summary>
public static class DeceasedRecordsGetAllMapping
{
    /// <summary>Маппит DTO листинга карточек в запрос use case.</summary>
    public static GetAllDeceasedQuery ToQuery(this GetAllDeceasedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetAllDeceasedQuery(
            request.Search,
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.Country,
            request.City,
            request.IsVerified,
            request.CreatedFrom,
            request.CreatedTo,
            request.BirthDate,
            request.DeathDate,
            request.Page,
            request.PageSize);
    }
}
