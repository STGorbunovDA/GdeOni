using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.IsTrackedByMe.UseCase;

public sealed class IsTrackedByMeUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IIsTrackedByMeUseCase
{
    public async Task<Result<IsTrackedByMeResponse, Error>> Execute(
        IsTrackedByMeQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetByIdWithTracking(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var tracking = user.GetTracking(query.DeceasedId);
        var tracked = tracking is not null && !tracking.IsArchived();

        return Result.Success<IsTrackedByMeResponse, Error>(new IsTrackedByMeResponse(tracked));
    }
}
