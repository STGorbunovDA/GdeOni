using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangePassword.UseCase;

public sealed class ChangePasswordUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangePasswordUseCase
{
    public Task<Result<ChangePasswordResponse, Error>> Execute(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<ChangePasswordResponse, Error>> Handle(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var isCurrentPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
            return Errors.User.CurrentPasswordInvalid();

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);

        var result = user.ChangePasswordHash(newPasswordHash);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangePasswordResponse, Error>(
            new ChangePasswordResponse(user.Id));
    }
}