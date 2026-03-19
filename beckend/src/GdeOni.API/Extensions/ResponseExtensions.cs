using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Extensions;

public static class ResponseExtensions
{
    public static ActionResult ToErrorResponse<T>(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(ApiResponse<T>.Error(error))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToOkResponse<T>(this T? result, int statusCode = StatusCodes.Status200OK)
    {
        return new ObjectResult(ApiResponse<T>.Ok(result))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToCreatedResponse<T>(this T result, string location)
    {
        return new CreatedResult(location, ApiResponse<T>.Ok(result));
    }
}