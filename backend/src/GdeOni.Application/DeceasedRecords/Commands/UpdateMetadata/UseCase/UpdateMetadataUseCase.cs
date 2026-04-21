using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;

public sealed class UpdateMetadataUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMetadataUseCase
{
    public Task<Result<UpdateMetadataResponse, Error>> Execute(
        UpdateMetadataCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateMetadataResponse, Error>> Handle(
        UpdateMetadataCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var metadataResult = DeceasedMetadata.Create(
            command.Epitaph,
            command.Religion,
            command.Source,
            command.IsMilitaryService,
            command.AdditionalInfo);

        if (metadataResult.IsFailure)
            return metadataResult.Error;

        var result = deceased.UpdateMetadata(metadataResult.Value);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMetadataResponse, Error>(
            new UpdateMetadataResponse(deceased.Id));
    }
}