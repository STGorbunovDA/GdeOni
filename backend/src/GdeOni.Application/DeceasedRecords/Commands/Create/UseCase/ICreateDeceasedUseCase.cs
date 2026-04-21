using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;

public interface ICreateDeceasedUseCase
{
    Task<Result<CreateDeceasedResponse, Error>> Execute(
        CreateDeceasedCommand command,
        CancellationToken cancellationToken);
}