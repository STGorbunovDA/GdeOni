using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangePassword.UseCase;

public sealed class ChangePasswordUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangePasswordUseCase
{
    public Task<Result<ChangePasswordResponse, Error>> Execute(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangePasswordResponse, Error>> Handle(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var isSelf = currentUserService.UserId.HasValue && currentUserService.UserId.Value == command.UserId;

        var mustVerifyCurrentPassword = !isAdmin || isSelf;

        if (mustVerifyCurrentPassword)
        {
            if (string.IsNullOrWhiteSpace(command.CurrentPassword))
                return Errors.User.CurrentPasswordInvalid();

            var isCurrentPasswordValid =
                passwordHasher.Verify(command.CurrentPassword, user.PasswordHash);

            if (!isCurrentPasswordValid)
                return Errors.User.CurrentPasswordInvalid();
        }

        var newPasswordHash = passwordHasher.Hash(command.NewPassword);

        var result = user.ChangePasswordHash(newPasswordHash);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangePasswordResponse, Error>(
            new ChangePasswordResponse(user.Id));
    }
}