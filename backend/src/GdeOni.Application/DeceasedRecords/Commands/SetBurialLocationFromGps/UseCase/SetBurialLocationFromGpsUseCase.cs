using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.UseCase;

public sealed class SetBurialLocationFromGpsUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ISetBurialLocationFromGpsUseCase
{
    public Task<Result<SetBurialLocationFromGpsResponse, Error>> Execute(
        SetBurialLocationFromGpsCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<SetBurialLocationFromGpsResponse, Error>> Handle(
        SetBurialLocationFromGpsCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.Deceased.SetBurialLocationForbidden();

        // Сохраняем адресные поля от существующего BurialLocation, если
        // оно есть — этим эндпойнтом теперь пользуется не только сценарий
        // "у могилы впервые задаём GPS", но и "поправить координаты"
        // (E20). Без preserve адреса при правке координат пользователь
        // терял бы Country/City/CemeteryName/PlotNumber/GraveNumber.
        var existing = deceased.BurialLocation;
        var burialLocationResult = BurialLocation.Create(
            latitude: command.Latitude,
            longitude: command.Longitude,
            country: existing?.Country,
            region: existing?.Region,
            city: existing?.City,
            cemeteryName: existing?.CemeteryName,
            plotNumber: existing?.PlotNumber,
            graveNumber: existing?.GraveNumber,
            accuracy: LocationAccuracy.Exact,
            accuracyMeters: command.AccuracyMeters);

        if (burialLocationResult.IsFailure)
            return burialLocationResult.Error;

        var changeResult = deceased.ChangeBurialLocation(burialLocationResult.Value);
        if (changeResult.IsFailure)
            return changeResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<SetBurialLocationFromGpsResponse, Error>(
            new SetBurialLocationFromGpsResponse(deceased.Id));
    }
}
