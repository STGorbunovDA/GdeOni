using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.AssignMissingLogins.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.AssignMissingLogins.UseCase;

public interface IAssignMissingLoginsUseCase
{
    Task<Result<AssignMissingLoginsResponse, Error>> Execute(
        AssignMissingLoginsCommand command,
        CancellationToken cancellationToken);
}
