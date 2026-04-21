using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetAll.UseCase;

public interface IGetAllUsersUseCase
{
    Task<Result<PagedResponse<GetAllUsersResponse>, Error>> Execute(
        GetAllUsersQuery query,
        CancellationToken cancellationToken);
}