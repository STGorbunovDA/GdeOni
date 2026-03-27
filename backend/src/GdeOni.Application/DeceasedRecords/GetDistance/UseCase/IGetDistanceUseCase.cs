using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetDistance.UseCase;

public interface IGetDistanceUseCase
{
    Task<Result<GetDistanceResponse, Error>> Execute(
        Guid deceasedId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}