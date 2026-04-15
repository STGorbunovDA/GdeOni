using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Delete.UseCase;

public interface IDeleteUserUseCase
{
    Task<Result<DeleteUserResponse, Error>> Execute(
        DeleteUserCommand command,
        CancellationToken cancellationToken);
}