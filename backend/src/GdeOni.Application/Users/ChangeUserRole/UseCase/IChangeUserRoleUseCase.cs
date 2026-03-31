using CSharpFunctionalExtensions;
using GdeOni.Application.Users.ChangeUserRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeUserRole.UseCase;

public interface IChangeUserRoleUseCase
{
    Task<Result<ChangeUserRoleResponse, Error>> Execute(
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken);
}