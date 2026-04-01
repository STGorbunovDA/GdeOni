using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Verify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Verify.UseCase;

public sealed class VerifyDeceasedUseCase(IDeceasedRepository deceasedRepository)
    : IVerifyDeceasedUseCase
{
    public async Task<Result<VerifyDeceasedResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        var verifyResult = deceased.Verify();
        if (verifyResult.IsFailure)
            return verifyResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<VerifyDeceasedResponse, Error>(
            new VerifyDeceasedResponse(deceased.Id, deceased.IsVerified));
    }
}