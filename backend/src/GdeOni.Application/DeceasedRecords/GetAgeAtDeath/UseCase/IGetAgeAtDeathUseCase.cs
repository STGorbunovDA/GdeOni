using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.GetAgeAtDeath.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetAgeAtDeath.UseCase;

public interface IGetAgeAtDeathUseCase
{
    Task<Result<GetAgeAtDeathResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}