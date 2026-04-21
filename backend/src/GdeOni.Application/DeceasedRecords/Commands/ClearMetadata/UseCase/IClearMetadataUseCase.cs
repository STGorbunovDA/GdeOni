using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;

public interface IClearMetadataUseCase
{
    Task<Result<ClearMetadataResponse, Error>> Execute(
        ClearMetadataCommand command,
        CancellationToken cancellationToken);
}