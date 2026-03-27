using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdateMetadata.UseCase;

public interface IUpdateMetadataUseCase
{
    Task<Result<UpdateMetadataResponse, Error>> Execute(
        UpdateMetadataRequest request,
        CancellationToken cancellationToken);
}