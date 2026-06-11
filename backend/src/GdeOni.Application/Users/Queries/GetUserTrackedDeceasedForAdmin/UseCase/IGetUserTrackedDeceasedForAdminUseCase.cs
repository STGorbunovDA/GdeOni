using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.UseCase;

public interface IGetUserTrackedDeceasedForAdminUseCase
{
    Task<Result<GetUserTrackedDeceasedForAdminResponse, Error>> Execute(
        GetUserTrackedDeceasedForAdminQuery query,
        CancellationToken cancellationToken);
}
