using CSharpFunctionalExtensions;
using GdeOni.API.Extensions;
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

    protected bool CanAccessUserResource(Guid targetUserId, Guid? currentUserId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        return currentUserId.HasValue && currentUserId.Value == targetUserId;
    }

    protected ActionResult? EnsureUserResourceAccess(Guid targetUserId, Guid? currentUserId, bool isAdmin)
    {
        return CanAccessUserResource(targetUserId, currentUserId, isAdmin)
            ? null
            : Forbid();
    }
}