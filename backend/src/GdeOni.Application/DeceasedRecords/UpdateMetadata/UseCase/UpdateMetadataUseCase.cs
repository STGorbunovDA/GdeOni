using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdateMetadata.UseCase;

public sealed class UpdateMetadataUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMetadataUseCase
{
    public Task<Result<UpdateMetadataResponse, Error>> Execute(
        UpdateMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdateMetadataResponse, Error>> Handle(
        UpdateMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var metadata = DeceasedMetadata.Create(
            request.Epitaph,
            request.Religion,
            request.Source,
            request.IsMilitaryService,
            request.AdditionalInfo);

        var result = deceased.UpdateMetadata(metadata);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMetadataResponse, Error>(
            new UpdateMetadataResponse(deceased.Id));
    }
}