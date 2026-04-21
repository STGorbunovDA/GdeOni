using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;

public interface IRemoveMemoryUseCase
{
    Task<UnitResult<Error>> Execute(Guid deceasedId, Guid memoryId, CancellationToken cancellationToken);
}