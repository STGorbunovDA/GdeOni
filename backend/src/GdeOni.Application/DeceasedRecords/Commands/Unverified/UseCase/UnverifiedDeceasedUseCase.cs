using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;

public sealed class UnverifiedDeceasedUseCase(IDeceasedRepository deceasedRepository)
    : IUnverifiedDeceasedUseCase
{
    public async Task<Result<UnverifiedDeceasedResponse, Error>> Execute(
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

        return Result.Success<UnverifiedDeceasedResponse, Error>(
            new UnverifiedDeceasedResponse(deceased.Id, deceased.IsVerified));
    }
}