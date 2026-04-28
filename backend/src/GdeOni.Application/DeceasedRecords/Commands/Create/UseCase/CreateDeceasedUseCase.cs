using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;

public sealed class CreateDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ICreateDeceasedUseCase
{
    public Task<Result<CreateDeceasedResponse, Error>> Execute(
        CreateDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreateDeceasedResponse, Error>> Handle(
        CreateDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        if (!isAdmin)
        {
            var creatorExists = await userRepository.ExistsById(currentUserId, cancellationToken);
            if (!creatorExists)
                return Errors.General.NotFound("user", currentUserId);
        }
        
        BurialLocation? burialLocation = null;
        if (command.BurialLocation is not null)
        {
            var burialLocationResult = BurialLocation.Create(
                command.BurialLocation.Latitude,
                command.BurialLocation.Longitude,
                command.BurialLocation.Country,
                command.BurialLocation.Region,
                command.BurialLocation.City,
                command.BurialLocation.CemeteryName,
                command.BurialLocation.PlotNumber,
                command.BurialLocation.GraveNumber,
                command.BurialLocation.Accuracy,
                command.BurialLocation.AccuracyMeters);

            if (burialLocationResult.IsFailure)
                return burialLocationResult.Error;

            burialLocation = burialLocationResult.Value;
        }

        var deceasedResult = Deceased.Create(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.BirthDate,
            command.DeathDate,
            burialLocation,
            currentUserId,
            command.ShortDescription,
            command.Biography);

        if (deceasedResult.IsFailure)
            return deceasedResult.Error;

        var deceased = deceasedResult.Value;

        var alreadyExists = await deceasedRepository.ExistsBySearchKey(deceased.SearchKey, cancellationToken);
        if (alreadyExists)
            return Errors.Deceased.AlreadyExists();

        if (command.Photos is not null)
        {
            foreach (var photo in command.Photos)
            {
                var addPhotoResult = deceased.AddPhoto(
                    photo.Url,
                    currentUserId,
                    photo.Description,
                    photo.IsPrimary);

                if (addPhotoResult.IsFailure)
                    return addPhotoResult.Error;
            }
        }

        if (command.Memories is not null)
        {
            foreach (var memory in command.Memories)
            {
                var addMemoryResult = deceased.AddMemory(memory.Text, currentUserId);
                if (addMemoryResult.IsFailure)
                    return addMemoryResult.Error;
            }
        }

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

        await deceasedRepository.Add(deceased, cancellationToken);
        await deceasedRepository.Save(cancellationToken);

        return Result.Success<CreateDeceasedResponse, Error>(
            new CreateDeceasedResponse(deceased.Id));
    }
}