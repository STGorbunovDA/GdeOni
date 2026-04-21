using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;

public interface IUpdateMemoryUseCase
{
    Task<Result<UpdateMemoryResponse, Error>> Execute(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken);
}