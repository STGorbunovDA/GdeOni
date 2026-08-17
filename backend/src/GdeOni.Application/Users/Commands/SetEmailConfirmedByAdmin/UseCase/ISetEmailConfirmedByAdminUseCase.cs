using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.SetEmailConfirmedByAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.SetEmailConfirmedByAdmin.UseCase;

public interface ISetEmailConfirmedByAdminUseCase
{
    Task<UnitResult<Error>> Execute(
        SetEmailConfirmedByAdminCommand command,
        CancellationToken cancellationToken);
}
