using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.ClearMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.ClearMetadata.UseCase;

public interface IClearMetadataUseCase
{
    Task<Result<ClearMetadataResponse, Error>> Execute(
        ClearMetadataRequest request,
        CancellationToken cancellationToken);
}