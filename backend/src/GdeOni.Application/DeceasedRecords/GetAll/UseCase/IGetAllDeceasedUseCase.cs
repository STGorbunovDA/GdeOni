using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetAll.UseCase;

public interface IGetAllDeceasedUseCase
{
    Task<Result<PagedResponse<DeceasedListItemResponse>, Error>> Execute(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken);
}