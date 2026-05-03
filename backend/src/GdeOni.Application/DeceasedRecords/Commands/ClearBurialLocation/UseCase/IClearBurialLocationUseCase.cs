using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.UseCase;

public interface IClearBurialLocationUseCase
{
    Task<Result<ClearBurialLocationResponse, Error>> Execute(
        ClearBurialLocationCommand command,
        CancellationToken cancellationToken);
}
