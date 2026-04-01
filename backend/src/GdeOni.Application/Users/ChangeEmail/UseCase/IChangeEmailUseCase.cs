using CSharpFunctionalExtensions;
using GdeOni.Application.Users.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeEmail.UseCase;

public interface IChangeEmailUseCase
{
    Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailRequest request,
        CancellationToken cancellationToken);
}