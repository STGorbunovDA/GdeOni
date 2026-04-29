using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.UseCase;

public interface IGetMyTrackedDeceasedListUseCase
{
    Task<Result<PagedResponse<MyTrackedDeceasedListItemResponse>, Error>> Execute(
        GetMyTrackedDeceasedListQuery query,
        CancellationToken cancellationToken);
}
