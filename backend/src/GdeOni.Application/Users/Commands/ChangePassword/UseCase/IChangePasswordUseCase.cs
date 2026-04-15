using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangePassword.UseCase;

public interface IChangePasswordUseCase
{
    Task<Result<ChangePasswordResponse, Error>> Execute(
        ChangePasswordCommand command,
        CancellationToken cancellationToken);
}