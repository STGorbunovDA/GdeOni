using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Unverify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Unverify.UseCase;

public interface IUnverifyDeceasedUseCase
{
    Task<Result<UnverifyDeceasedResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}