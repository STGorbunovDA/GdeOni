using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.UseCase;

public sealed class UpdateUserProfileUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateUserProfileUseCase
{
    public Task<Result<UpdateUserProfileResponse, Error>> Execute(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateUserProfileResponse, Error>> Handle(
        UpdateUserProfileCommand command,
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

        var userNameExists = await userRepository.ExistsByUserName(command.UserName, cancellationToken);
        if (userNameExists && !string.Equals(user.UserName, command.UserName.Trim(), StringComparison.OrdinalIgnoreCase))
            return Errors.User.UserNameAlreadyExists();

        var result = user.UpdateProfile(command.UserName, command.FullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<UpdateUserProfileResponse, Error>(
            new UpdateUserProfileResponse(user.Id));
    }
}