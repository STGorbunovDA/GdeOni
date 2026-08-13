using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.ChangeLogin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeLogin.UseCase;

public interface IChangeLoginUseCase
{
    Task<UnitResult<Error>> Execute(
        ChangeLoginCommand command,
        CancellationToken cancellationToken);
}
