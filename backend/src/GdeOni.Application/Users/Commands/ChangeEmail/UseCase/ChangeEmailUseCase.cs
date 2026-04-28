using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeEmail.UseCase;

public sealed class ChangeEmailUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeEmailUseCase
{
    public Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangeEmailResponse, Error>> Handle(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();
        
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        if (!isAdmin && user.Id != currentUserId)
            return Errors.User.UserForbidden();
        
        var exists = await userRepository.ExistsByEmail(command.Email, cancellationToken);
        if (exists && !string.Equals(user.Email, command.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return Errors.User.EmailAlreadyExists();

        var result = user.ChangeEmail(command.Email);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangeEmailResponse, Error>(
            new ChangeEmailResponse(user.Id, user.Email));
    }
}