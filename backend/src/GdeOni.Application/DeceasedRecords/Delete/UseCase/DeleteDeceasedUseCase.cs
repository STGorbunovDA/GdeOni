using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Delete.UseCase;

public sealed class DeleteDeceasedUseCase(IDeceasedRepository deceasedRepository)
    : IDeleteDeceasedUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(id, cancellationToken);

        if (deceased is null)
            return Errors.General.NotFound("deceased", id);

        deceasedRepository.Delete(deceased);
        await deceasedRepository.Save(cancellationToken);

        return UnitResult.Success<Error>();
    }
}