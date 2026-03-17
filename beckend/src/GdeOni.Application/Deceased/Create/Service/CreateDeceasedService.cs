using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Domain.Aggregates.Deceased;

namespace GdeOni.Application.Deceased.Create.Service;

public sealed class CreateDeceasedService : ICreateDeceasedService
{
    private readonly IDeceasedRepository _deceasedRepository;
    private readonly IUserRepository _userRepository;

    public CreateDeceasedService(
        IDeceasedRepository deceasedRepository,
        IUserRepository userRepository)
    {
        _deceasedRepository = deceasedRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CreateDeceasedResponse>> ExecuteAsync(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Result.Failure<CreateDeceasedResponse>("Request обязателен");

        if (request.BurialLocation is null)
            return Result.Failure<CreateDeceasedResponse>("BurialLocation обязателен");

        var creatorExists = await _userRepository.ExistsByIdAsync(
            request.CreatedByUserId,
            cancellationToken);

        if (!creatorExists)
            return Result.Failure<CreateDeceasedResponse>("Пользователь-создатель не найден");

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
            return Result.Failure<CreateDeceasedResponse>(burialLocationResult.Error);

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
            return Result.Failure<CreateDeceasedResponse>(deceasedResult.Error);

        var deceased = deceasedResult.Value;

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
                    return Result.Failure<CreateDeceasedResponse>(addPhotoResult.Error);
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
                    return Result.Failure<CreateDeceasedResponse>(addMemoryResult.Error);
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
                return Result.Failure<CreateDeceasedResponse>(metadataResult.Error);
        }

        await _deceasedRepository.AddAsync(deceased, cancellationToken);
        await _deceasedRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDeceasedResponse(deceased.Id));
    }
}