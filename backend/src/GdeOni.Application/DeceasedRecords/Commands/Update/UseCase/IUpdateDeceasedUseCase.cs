using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;

public interface IUpdateDeceasedUseCase
{
    Task<Result<UpdateDeceasedResponse, Error>> Execute(
        UpdateDeceasedCommand command,
        CancellationToken cancellationToken);
}