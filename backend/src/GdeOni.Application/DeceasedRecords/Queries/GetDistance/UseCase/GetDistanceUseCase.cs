using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;

public sealed class GetDistanceUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetDistanceUseCase
{
    public Task<Result<GetDistanceResponse, Error>> Execute(
        GetDistanceQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetDistanceResponse, Error>> Handle(
        GetDistanceQuery query,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        var distance = deceased.BurialLocation.DistanceTo(query.Latitude, query.Longitude);

        return Result.Success<GetDistanceResponse, Error>(
            new GetDistanceResponse(
                deceased.Id,
                query.Latitude,
                query.Longitude,
                distance));
    }
}