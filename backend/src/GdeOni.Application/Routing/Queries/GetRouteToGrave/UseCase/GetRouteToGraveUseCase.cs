using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;

public sealed class GetRouteToGraveUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    IEnumerable<IRouteLinkProvider> routeLinkProviders,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetRouteToGraveUseCase
{
    public Task<Result<GetRouteToGraveResult, Error>> Execute(
        GetRouteToGraveQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetRouteToGraveResult, Error>> Handle(
        GetRouteToGraveQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // EXISTS-проверка через subquery — без подгрузки User или его
        // коллекции tracking. IsActivelyTracking уже отсекает Archived.
        var isTracking = await userRepository.IsActivelyTracking(
            currentUserIdResult.Value, query.DeceasedId, cancellationToken);

        if (!isTracking)
            return Errors.Tracking.NotTracked();

        var deceased = await deceasedRepository.GetByIdReadOnly(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        if (deceased.BurialLocation is null)
            return Errors.Deceased.BurialLocationNotSet();

        var burial = deceased.BurialLocation;

        var links = routeLinkProviders
            .Select(provider => new RouteLink(
                provider.ProviderKey,
                provider.BuildLink(query.FromLat, query.FromLon, burial.Latitude, burial.Longitude, query.Mode)))
            .ToArray();

        return Result.Success<GetRouteToGraveResult, Error>(
            new GetRouteToGraveResult(
                deceased.Id,
                burial.Latitude,
                burial.Longitude,
                burial.AccuracyMeters,
                query.FromLat,
                query.FromLon,
                query.Mode,
                links));
    }
}
