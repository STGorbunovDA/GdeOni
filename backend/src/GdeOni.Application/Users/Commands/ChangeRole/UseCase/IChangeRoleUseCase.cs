using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeRole.UseCase;

public interface IChangeRoleUseCase
{
    Task<Result<ChangeRoleResponse, Error>> Execute(
        ChangeRoleCommand command,
        CancellationToken cancellationToken);
}