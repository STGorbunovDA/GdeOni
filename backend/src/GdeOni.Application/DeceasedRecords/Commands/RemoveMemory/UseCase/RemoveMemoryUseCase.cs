using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;

public sealed class RemoveMemoryUseCase(IDeceasedRepository deceasedRepository)
    : IRemoveMemoryUseCase
{
    public async Task<UnitResult<Error>> Execute(
        Guid deceasedId,
        Guid memoryId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        var result = deceased.RemoveMemory(memoryId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}