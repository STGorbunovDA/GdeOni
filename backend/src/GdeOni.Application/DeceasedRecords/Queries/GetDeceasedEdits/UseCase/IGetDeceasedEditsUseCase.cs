using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.UseCase;

public interface IGetDeceasedEditsUseCase
{
    Task<Result<GetDeceasedEditsResponse, Error>> Execute(
        GetDeceasedEditsQuery query,
        CancellationToken cancellationToken);
}
