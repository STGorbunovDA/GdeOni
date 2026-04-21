using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;

public interface IAddMemoryUseCase
{
    Task<Result<AddMemoryResponse, Error>> Execute(
        AddMemoryCommand command,
        CancellationToken cancellationToken);
}