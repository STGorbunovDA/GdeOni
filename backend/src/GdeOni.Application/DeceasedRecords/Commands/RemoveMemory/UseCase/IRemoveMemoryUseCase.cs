using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;

public interface IRemoveMemoryUseCase
{
    Task<UnitResult<Error>> Execute(
        RemoveMemoryCommand command,
        CancellationToken cancellationToken);
}