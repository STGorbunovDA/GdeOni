using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;

public sealed class UpdateDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateDeceasedUseCase
{
    public Task<Result<UpdateDeceasedResponse, Error>> Execute(
        UpdateDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateDeceasedResponse, Error>> Handle(
        UpdateDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        var deceased = await deceasedRepository.GetById(command.Id, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.Id);

        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.Deceased.UpdateForbidden();

        var updateMainInfoResult = deceased.UpdateMainInfo(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.BirthDate,
            command.DeathDate,
            command.ShortDescription,
            command.Biography);

        if (updateMainInfoResult.IsFailure)
            return updateMainInfoResult.Error;

        var burialLocationResult = BurialLocation.Create(
            command.BurialLocation.Latitude,
            command.BurialLocation.Longitude,
            command.BurialLocation.Country,
            command.BurialLocation.Region,
            command.BurialLocation.City,
            command.BurialLocation.CemeteryName,
            command.BurialLocation.PlotNumber,
            command.BurialLocation.GraveNumber,
            command.BurialLocation.Accuracy);

        if (burialLocationResult.IsFailure)
            return burialLocationResult.Error;

        var changeBurialLocationResult = deceased.ChangeBurialLocation(burialLocationResult.Value);
        if (changeBurialLocationResult.IsFailure)
            return changeBurialLocationResult.Error;

        if (command.Metadata is not null)
        {
            var metadataResult = DeceasedMetadata.Create(
                command.Metadata.Epitaph,
                command.Metadata.Religion,
                command.Metadata.Source,
                command.Metadata.IsMilitaryService,
                command.Metadata.AdditionalInfo);

            if (metadataResult.IsFailure)
                return metadataResult.Error;

            var updateMetadataResult = deceased.UpdateMetadata(metadataResult.Value);
            if (updateMetadataResult.IsFailure)
                return updateMetadataResult.Error;
        }

        await deceasedRepository.Save(cancellationToken);
        return Result.Success<UpdateDeceasedResponse, Error>(
            new UpdateDeceasedResponse(deceased.Id));
    }
}