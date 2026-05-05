using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;

public interface IGetDeceasedByIdUseCase
{
    Task<Result<GetDeceasedByIdResult, Error>> Execute(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken);
}
