using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;

public interface IGetDistanceUseCase
{
    Task<Result<GetDistanceResponse, Error>> Execute(
        GetDistanceQuery query,
        CancellationToken cancellationToken);
}