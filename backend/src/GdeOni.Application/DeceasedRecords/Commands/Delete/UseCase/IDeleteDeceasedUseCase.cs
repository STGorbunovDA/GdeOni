using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;

public interface IDeleteDeceasedUseCase
{
    Task<UnitResult<Error>> Execute(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken);
}