using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;

public interface IGetDistanceUseCase
{
    Task<Result<GetDistanceResponse, Error>> Execute(
        Guid deceasedId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}