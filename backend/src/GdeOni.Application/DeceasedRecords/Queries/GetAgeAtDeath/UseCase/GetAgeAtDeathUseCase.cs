using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;

public sealed class GetAgeAtDeathUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAgeAtDeathUseCase
{
    public Task<Result<GetAgeAtDeathResponse, Error>> Execute(
        GetAgeAtDeathQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetAgeAtDeathResponse, Error>> Handle(
        GetAgeAtDeathQuery query,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        return Result.Success<GetAgeAtDeathResponse, Error>(
            new GetAgeAtDeathResponse(
                deceased.Id,
                deceased.AgeAtDeath()));
    }
}