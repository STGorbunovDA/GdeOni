using GdeOni.API.Models;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.Common.Shared;
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

    public static PagedResponse<DeceasedListItemDto> ToDto(
        this PagedResponse<GetAllDeceasedItemResponse> response)
    {
        return new PagedResponse<DeceasedListItemDto>
        {
            Items = response.Items
                .Select(x => new DeceasedListItemDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    BirthDate = x.BirthDate,
                    DeathDate = x.DeathDate,
                    Country = x.Country,
                    City = x.City,
                    CemeteryName = x.CemeteryName,
                    PlotNumber = x.PlotNumber,
                    GraveNumber = x.GraveNumber,
                    IsVerified = x.IsVerified,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList(),
            TotalCount = response.TotalCount,
            Page = response.Page,
            PageSize = response.PageSize
        };
    }
}