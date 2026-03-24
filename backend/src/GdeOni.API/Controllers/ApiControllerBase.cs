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
            return result.Error.ToErrorResponse<T>();

        if (onSuccess is not null)
            return onSuccess(result.Value);

        return result.Value.ToOkResponse();
    }
}