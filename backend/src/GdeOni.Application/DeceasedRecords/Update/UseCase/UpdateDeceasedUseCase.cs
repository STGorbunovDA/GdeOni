using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Update.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Update.UseCase;

public sealed class UpdateDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateDeceasedUseCase
{
    public Task<Result<UpdateDeceasedResponse, Error>> Execute(
        UpdateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdateDeceasedResponse, Error>> Handle(
        UpdateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.Id, cancellationToken);

        if (deceased is null)
            return Errors.General.NotFound("deceased", request.Id);

        var updateMainInfoResult = deceased.UpdateMainInfo(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.BirthDate,
            request.DeathDate,
            request.ShortDescription,
            request.Biography);

        if (updateMainInfoResult.IsFailure)
            return updateMainInfoResult.Error;

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

        var changeBurialLocationResult = deceased.ChangeBurialLocation(burialLocationResult.Value);
        if (changeBurialLocationResult.IsFailure)
            return changeBurialLocationResult.Error;

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
            await deceasedRepository.Save(cancellationToken);
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintName == DbConstraints.DeceasedSearchKey)
        {
            return Errors.Deceased.AlreadyExists();
        }

        return Result.Success<UpdateDeceasedResponse, Error>(
            new UpdateDeceasedResponse(deceased.Id));
    }
}