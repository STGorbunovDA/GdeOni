using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.UseCase;

public sealed class AddDeceasedAtGraveUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAddDeceasedAtGraveUseCase
{
    public Task<Result<AddDeceasedAtGraveResponse, Error>> Execute(
        AddDeceasedAtGraveCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<AddDeceasedAtGraveResponse, Error>> Handle(
        AddDeceasedAtGraveCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;

        var user = await userRepository.GetByIdWithTracking(currentUserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserId);

        var burialLocationResult = BurialLocation.Create(
            latitude: command.GraveLocation.Latitude,
            longitude: command.GraveLocation.Longitude,
            country: command.GraveLocation.Country,
            city: command.GraveLocation.City,
            cemeteryName: command.GraveLocation.CemeteryName,
            plotNumber: command.GraveLocation.PlotNumber,
            graveNumber: command.GraveLocation.GraveNumber,
            accuracyMeters: command.GraveLocation.AccuracyMeters);

        if (burialLocationResult.IsFailure)
            return burialLocationResult.Error;

        var deceasedResult = Deceased.Create(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.BirthDate,
            command.DeathDate,
            burialLocationResult.Value,
            currentUserId,
            command.ShortDescription,
            command.Biography);

        if (deceasedResult.IsFailure)
            return deceasedResult.Error;

        var deceased = deceasedResult.Value;

        var alreadyExists = await deceasedRepository.ExistsBySearchKey(deceased.SearchKey, cancellationToken);
        if (alreadyExists)
            return Errors.Deceased.AlreadyExists();

        var trackingResult = user.TrackDeceased(
            deceased.Id,
            command.Tracking.RelationshipType,
            command.Tracking.PersonalNotes,
            command.Tracking.NotifyOnDeathAnniversary,
            command.Tracking.NotifyOnBirthAnniversary);

        if (trackingResult.IsFailure)
            return trackingResult.Error;

        await deceasedRepository.Add(deceased, cancellationToken);
        await deceasedRepository.Save(cancellationToken);

        var tracking = trackingResult.Value;

        return Result.Success<AddDeceasedAtGraveResponse, Error>(
            new AddDeceasedAtGraveResponse(
                deceased.Id,
                tracking.Id,
                tracking.Status.ToString()));
    }
}
