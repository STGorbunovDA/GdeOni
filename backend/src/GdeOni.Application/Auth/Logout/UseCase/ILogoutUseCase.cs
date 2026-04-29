using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Logout.UseCase;

public interface ILogoutUseCase
{
    Task<UnitResult<Error>> Execute(
        LogoutCommand command,
        CancellationToken cancellationToken);
}
