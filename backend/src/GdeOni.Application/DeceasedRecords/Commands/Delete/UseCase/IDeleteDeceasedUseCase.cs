using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;

public interface IDeleteDeceasedUseCase
{
    Task<Result<DeleteDeceasedResponse, Error>> Execute(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken);
}