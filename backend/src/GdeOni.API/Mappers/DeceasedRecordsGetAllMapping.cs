using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

namespace GdeOni.API.Mappers;

public static class DeceasedRecordsGetAllMapping
{
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
