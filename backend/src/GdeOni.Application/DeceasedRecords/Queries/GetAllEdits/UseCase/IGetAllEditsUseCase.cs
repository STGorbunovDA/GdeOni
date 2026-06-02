using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.UseCase;

public interface IGetAllEditsUseCase
{
    Task<Result<GetAllEditsResponse, Error>> Execute(GetAllEditsQuery query, CancellationToken cancellationToken);
}
