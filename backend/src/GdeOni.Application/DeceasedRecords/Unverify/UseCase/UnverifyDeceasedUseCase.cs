using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Unverify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Unverify.UseCase;

public sealed class UnverifyDeceasedUseCase(IDeceasedRepository deceasedRepository)
    : IUnverifyDeceasedUseCase
{
    public async Task<Result<UnverifyDeceasedResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        var result = deceased.Unverify();
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UnverifyDeceasedResponse, Error>(
            new UnverifyDeceasedResponse(deceased.Id, deceased.IsVerified));
    }
}