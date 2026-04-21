using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;

public interface IUpdateMetadataUseCase
{
    Task<Result<UpdateMetadataResponse, Error>> Execute(
        UpdateMetadataCommand command,
        CancellationToken cancellationToken);
}