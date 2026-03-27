using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Delete.UseCase;

public interface IDeleteDeceasedUseCase
{
    Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken);
}