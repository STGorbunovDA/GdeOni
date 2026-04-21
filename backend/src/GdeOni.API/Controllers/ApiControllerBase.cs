using CSharpFunctionalExtensions;
using GdeOni.API.Extensions;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromResult<T>(
        Result<T, Error> result,
        Func<T, ActionResult>? onSuccess = null)
    {
        if (result.IsFailure)
            return result.Error.ToErrorResponse();

        if (onSuccess is not null)
            return onSuccess(result.Value);

        return result.Value.ToOkResponse();
    }

    protected ActionResult FromUnitResult(
        UnitResult<Error> result,
        Func<ActionResult>? onSuccess = null)
    {
        if (result.IsFailure)
            return result.Error.ToErrorResponse();

        if (onSuccess is not null)
            return onSuccess();

        return NoContent();
    }

    protected Result<Guid, Error> GetRequiredCurrentUserId(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Result.Failure<Guid, Error>(
                Error.Unauthorized("auth.unauthorized", "Authentication is required."));
        }

        return Result.Success<Guid, Error>(currentUserService.UserId.Value);
    }

    protected bool CanAccessUserResource(Guid targetUserId, Guid currentUserId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        return currentUserId == targetUserId;
    }

    protected ActionResult? EnsureUserResourceAccess(Guid targetUserId, Guid currentUserId, bool isAdmin)
    {
        return CanAccessUserResource(targetUserId, currentUserId, isAdmin)
            ? null
            : Error.Forbidden("auth.forbidden", "You do not have access to this resource.")
                .ToErrorResponse();
    }
}