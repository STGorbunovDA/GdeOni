using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.Unblock.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Unblock.UseCase;

public interface IUnblockUserUseCase
{
    Task<Result<UnblockUserResponse, Error>> Execute(
        UnblockUserCommand command,
        CancellationToken cancellationToken);
}
