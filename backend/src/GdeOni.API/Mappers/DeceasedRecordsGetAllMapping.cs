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
            request.Country,
            request.City,
            request.IsVerified,
            request.CreatedFrom,
            request.CreatedTo,
            request.Page,
            request.PageSize);
    }
}
