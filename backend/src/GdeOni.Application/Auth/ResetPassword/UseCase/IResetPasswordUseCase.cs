using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.ResetPassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ResetPassword.UseCase;

public interface IResetPasswordUseCase
{
    Task<UnitResult<Error>> Execute(
        ResetPasswordCommand command,
        CancellationToken cancellationToken);
}
