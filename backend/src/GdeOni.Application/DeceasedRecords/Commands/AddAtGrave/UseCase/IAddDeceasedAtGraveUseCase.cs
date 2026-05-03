using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.UseCase;

public interface IAddDeceasedAtGraveUseCase
{
    Task<Result<AddDeceasedAtGraveResponse, Error>> Execute(
        AddDeceasedAtGraveCommand command,
        CancellationToken cancellationToken);
}
