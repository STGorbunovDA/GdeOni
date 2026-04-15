using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeEmail.UseCase;

public interface IChangeEmailUseCase
{
    Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailCommand command,
        CancellationToken cancellationToken);
}