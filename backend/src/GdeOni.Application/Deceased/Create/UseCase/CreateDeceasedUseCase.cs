using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Deceased.Create.UseCase;

public sealed class CreateDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ICreateDeceasedUseCase
{
    public Task<Result<CreateDeceasedResponse, Error>> Execute(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(
            request,
            Handle,
            cancellationToken);
    }

    private async Task<Result<CreateDeceasedResponse, Error>> Handle(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        var creatorExists = await userRepository.ExistsById(
            request.CreatedByUserId,
            cancellationToken);

        if (!creatorExists)
            return Errors.General.NotFound("user", request.CreatedByUserId);

        var burialLocationResult = BurialLocation.Create(
            request.BurialLocation.Latitude,
            request.BurialLocation.Longitude,
            request.BurialLocation.Country,
            request.BurialLocation.Region,
            request.BurialLocation.City,
            request.BurialLocation.CemeteryName,
            request.BurialLocation.PlotNumber,
            request.BurialLocation.GraveNumber,
            request.BurialLocation.Accuracy);

        if (burialLocationResult.IsFailure)
            return burialLocationResult.Error;

        var deceasedResult = Domain.Aggregates.Deceased.Deceased.Create(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.BirthDate,
            request.DeathDate,
            burialLocationResult.Value,
            request.CreatedByUserId,
            request.ShortDescription,
            request.Biography);

        if (deceasedResult.IsFailure)
            return deceasedResult.Error;

        var deceased = deceasedResult.Value;

        var alreadyExists = await deceasedRepository.ExistsBySearchKey(
            deceased.SearchKey,
            cancellationToken);

        if (alreadyExists)
            return Errors.Deceased.AlreadyExists();

        if (request.Photos is not null)
        {
            foreach (var photo in request.Photos)
            {
                var addPhotoResult = deceased.AddPhoto(
                    photo.Url,
                    photo.AddedByUserId,
                    photo.Description,
                    photo.IsPrimary);

                if (addPhotoResult.IsFailure)
                    return addPhotoResult.Error;
            }
        }

        if (request.Memories is not null)
        {
            foreach (var memory in request.Memories)
            {
                var addMemoryResult = deceased.AddMemory(
                    memory.Text,
                    memory.AuthorDisplayName,
                    memory.AuthorUserId);

                if (addMemoryResult.IsFailure)
                    return addMemoryResult.Error;
            }
        }

        if (request.Metadata is not null)
        {
            var metadata = DeceasedMetadata.Create(
                request.Metadata.Epitaph,
                request.Metadata.Religion,
                request.Metadata.Source,
                request.Metadata.IsMilitaryService,
                request.Metadata.AdditionalInfo);

            var metadataResult = deceased.UpdateMetadata(metadata);

            if (metadataResult.IsFailure)
                return metadataResult.Error;
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