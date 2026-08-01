using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.ResendConfirmation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ResendConfirmation.UseCase;

public interface IResendEmailConfirmationUseCase
{
    Task<UnitResult<Error>> Execute(
        ResendEmailConfirmationCommand command,
        CancellationToken cancellationToken);
}
