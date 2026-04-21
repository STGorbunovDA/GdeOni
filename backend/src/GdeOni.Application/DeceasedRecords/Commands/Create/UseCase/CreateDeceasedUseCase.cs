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
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
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
        var isSuperAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString());

        if (!isSuperAdmin)
        {
            var creatorExists = await userRepository.ExistsById(command.CreatedByUserId, cancellationToken);
            if (!creatorExists)
                return Errors.General.NotFound("user", command.CreatedByUserId);
        }

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

        var deceasedResult = Deceased.Create(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.BirthDate,
            command.DeathDate,
            burialLocationResult.Value,
            command.CreatedByUserId,
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
                if (!isSuperAdmin)
                {
                    var addedByUserExists = await userRepository.ExistsById(photo.AddedByUserId, cancellationToken);
                    if (!addedByUserExists)
                        return Errors.General.NotFound("user", photo.AddedByUserId);
                }
                
                var addPhotoResult = deceased.AddPhoto(
                    photo.Url,
                    photo.AddedByUserId,
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
                if (memory.AuthorUserId.HasValue)
                {
                    if (!isSuperAdmin)
                    {
                        var authorExists = await userRepository.ExistsById(memory.AuthorUserId.Value, cancellationToken);
                        if (!authorExists)
                            return Errors.General.NotFound("user", memory.AuthorUserId.Value);
                    }
                }

                var addMemoryResult = deceased.AddMemory(
                    memory.Text,
                    memory.AuthorUserId);

                if (addMemoryResult.IsFailure)
                    return addMemoryResult.Error;
            }
        }

        if (command.Metadata is not null)
        {
            var metadata = DeceasedMetadata.Create(
                command.Metadata.Epitaph,
                command.Metadata.Religion,
                command.Metadata.Source,
                command.Metadata.IsMilitaryService,
                command.Metadata.AdditionalInfo);

            var updateMetadataResult = deceased.UpdateMetadata(metadata);
            if (updateMetadataResult.IsFailure)
                return updateMetadataResult.Error;
        }

        try
        {
            await deceasedRepository.Add(deceased, cancellationToken);
            await deceasedRepository.Save(cancellationToken);
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintName == DbConstraints.DeceasedSearchKey)
        {
            return Errors.Deceased.AlreadyExists();
        }

        return Result.Success<CreateDeceasedResponse, Error>(
            new CreateDeceasedResponse(deceased.Id));
    }
}