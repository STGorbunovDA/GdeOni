using CSharpFunctionalExtensions;
using GdeOni.Application.Users.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangePassword.UseCase;

public interface IChangePasswordUseCase
{
    Task<Result<ChangePasswordResponse, Error>> Execute(
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}