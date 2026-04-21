using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Extensions;

public static class ResponseExtensions
{
    public static ActionResult ToErrorResponse(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(ApiResponse<object?>.Error(error))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToOkResponse<T>(this T result, int statusCode = StatusCodes.Status200OK)
    {
        return new ObjectResult(ApiResponse<T>.Success(result))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToCreatedResponse<T>(this T result, string location)
    {
        return new CreatedResult(location, ApiResponse<T>.Success(result));
    }
}