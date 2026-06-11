using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.UseCase;

public interface IGetNearbyDeceasedUseCase
{
    Task<Result<PagedResponse<NearbyDeceasedItemResponse>, Error>> Execute(
        GetNearbyDeceasedQuery query,
        CancellationToken cancellationToken);
}
