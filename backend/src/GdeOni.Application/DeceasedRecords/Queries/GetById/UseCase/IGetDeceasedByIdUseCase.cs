using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;

public interface IGetDeceasedByIdUseCase
{
    Task<Result<DeceasedDetailsResponse, Error>> Execute(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken);
}