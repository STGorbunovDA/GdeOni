using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.ConfirmEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ConfirmEmail.UseCase;

public interface IConfirmEmailUseCase
{
    Task<UnitResult<Error>> Execute(ConfirmEmailCommand command, CancellationToken cancellationToken);
}
