using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.ChangeFullName.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeFullName.UseCase;

public interface IChangeFullNameUseCase
{
    Task<UnitResult<Error>> Execute(
        ChangeFullNameCommand command,
        CancellationToken cancellationToken);
}
