using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;

public sealed class GetDistanceUseCase(IDeceasedRepository deceasedRepository)
    : IGetDistanceUseCase
{
    public async Task<Result<GetDistanceResponse, Error>> Execute(
        Guid deceasedId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        var distance = deceased.BurialLocation.DistanceTo(latitude, longitude);

        return Result.Success<GetDistanceResponse, Error>(
            new GetDistanceResponse(
                deceased.Id,
                latitude,
                longitude,
                distance));
    }
}