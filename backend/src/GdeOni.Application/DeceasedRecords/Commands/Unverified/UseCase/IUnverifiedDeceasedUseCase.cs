using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;

public interface IUnverifiedDeceasedUseCase
{
    Task<Result<UnverifiedDeceasedResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}