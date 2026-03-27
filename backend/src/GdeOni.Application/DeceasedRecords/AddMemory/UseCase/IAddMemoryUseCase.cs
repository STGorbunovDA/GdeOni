using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.AddMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.AddMemory.UseCase;

public interface IAddMemoryUseCase
{
    Task<Result<AddMemoryResponse, Error>> Execute(
        AddMemoryRequest request,
        CancellationToken cancellationToken);
}