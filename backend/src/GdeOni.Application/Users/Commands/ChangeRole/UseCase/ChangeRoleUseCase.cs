using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeRole.UseCase;

public sealed class ChangeRoleUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeRoleUseCase
{
    public Task<Result<ChangeRoleResponse, Error>> Execute(
        ChangeRoleCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangeRoleResponse, Error>> Handle(
        ChangeRoleCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();
        
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        var result = user.ChangeRole(command.UserRole);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangeRoleResponse, Error>(
            new ChangeRoleResponse(user.Id));
    }
}