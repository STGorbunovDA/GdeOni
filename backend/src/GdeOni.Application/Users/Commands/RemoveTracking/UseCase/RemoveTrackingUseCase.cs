using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Application.Users.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.RemoveTracking.UseCase;

public sealed class RemoveTrackingUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRemoveTrackingUseCase
{
    public Task<Result<RemoveTrackingResponse, Error>> Execute(
        RemoveTrackingCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RemoveTrackingResponse, Error>> Handle(
        RemoveTrackingCommand command,
        CancellationToken cancellationToken)
    {
        var accessError = UserAccessGuard.EnsureCanAccessUser(command.UserId, currentUserService);
        if (accessError is not null)
            return accessError;

        var user = await userRepository.GetByIdWithTracking(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        var result = user.RemoveTracking(command.DeceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<RemoveTrackingResponse, Error>(
            new RemoveTrackingResponse
            {
                UserId = user.Id,
                DeceasedId = command.DeceasedId,
                Removed = true
            });
    }
}