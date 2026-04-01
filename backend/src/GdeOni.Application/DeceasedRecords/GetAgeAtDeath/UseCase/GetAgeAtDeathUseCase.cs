using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.GetAgeAtDeath.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetAgeAtDeath.UseCase;

public sealed class GetAgeAtDeathUseCase(IDeceasedRepository deceasedRepository)
    : IGetAgeAtDeathUseCase
{
    public async Task<Result<GetAgeAtDeathResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        return Result.Success<GetAgeAtDeathResponse, Error>(
            new GetAgeAtDeathResponse(deceased.Id, deceased.AgeAtDeath()));
    }
}