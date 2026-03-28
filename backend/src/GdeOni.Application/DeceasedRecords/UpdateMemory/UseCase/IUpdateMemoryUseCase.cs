using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdateMemory.UseCase;

public interface IUpdateMemoryUseCase
{
    Task<Result<UpdateMemoryResponse, Error>> Execute(
        UpdateMemoryRequest request,
        CancellationToken cancellationToken);
}