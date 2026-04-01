using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Verify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Verify.UseCase;

public interface IVerifyDeceasedUseCase
{
    Task<Result<VerifyDeceasedResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}