using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Register.UseCase;

public interface IRegisterUserUseCase
{
    Task<Result<RegisterUserResponse, Error>> Execute(
        RegisterUserCommand command,
        CancellationToken cancellationToken);
}