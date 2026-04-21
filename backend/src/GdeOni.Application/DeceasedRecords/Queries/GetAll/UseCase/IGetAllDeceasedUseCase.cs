using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;

public interface IGetAllDeceasedUseCase
{
    Task<Result<PagedResponse<GetAllDeceasedItemResponse>, Error>> Execute(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken);
}