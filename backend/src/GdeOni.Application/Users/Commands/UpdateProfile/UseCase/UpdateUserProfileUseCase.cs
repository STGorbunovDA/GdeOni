using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.UseCase;

public sealed class UpdateUserProfileUseCase(
    IUserRepository userRepository,
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
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        var normalizedUserName = command.UserName.Trim().ToLowerInvariant();
        var normalizedFullName = string.IsNullOrWhiteSpace(command.FullName)
            ? null
            : command.FullName.Trim();

        var existingUserName = await userRepository.ExistsByUserName(normalizedUserName, cancellationToken);
        if (existingUserName && !string.Equals(user.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
            return Errors.User.UserNameAlreadyExists();

        var result = user.UpdateProfile(normalizedUserName, normalizedFullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<UpdateUserProfileResponse, Error>(
            new UpdateUserProfileResponse(user.Id));
    }
}