using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.UpdateProfile.UseCase;

public sealed class UpdateUserProfileUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateUserProfileUseCase
{
    public Task<Result<UpdateUserProfileResponse, Error>> Execute(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdateUserProfileResponse, Error>> Handle(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var existingUserName = await userRepository.ExistsByUserName(request.UserName, cancellationToken);
        if (existingUserName && !string.Equals(user.UserName, request.UserName.Trim(), StringComparison.OrdinalIgnoreCase))
            return Errors.User.UserNameAlreadyExists();

        var result = user.UpdateProfile(request.UserName, request.FullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<UpdateUserProfileResponse, Error>(
            new UpdateUserProfileResponse(user.Id));
    }
}