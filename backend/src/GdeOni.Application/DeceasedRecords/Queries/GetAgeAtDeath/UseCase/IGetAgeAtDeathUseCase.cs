using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;

public interface IGetAgeAtDeathUseCase
{
    Task<Result<GetAgeAtDeathResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}