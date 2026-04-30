using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.UseCase;

public interface IDeleteMediaUseCase
{
    Task<UnitResult<Error>> Execute(
        DeleteMediaCommand command,
        CancellationToken cancellationToken);
}
