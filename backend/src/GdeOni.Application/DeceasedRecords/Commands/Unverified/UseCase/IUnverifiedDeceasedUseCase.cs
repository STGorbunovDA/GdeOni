using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;

public interface IUnverifiedDeceasedUseCase
{
    Task<Result<UnverifyDeceasedResponse, Error>> Execute(
        UnverifyDeceasedCommand command,
        CancellationToken cancellationToken);
}