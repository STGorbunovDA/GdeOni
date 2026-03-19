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
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(Envelope.Error(error))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToOkResponse(this object? result, int statusCode = StatusCodes.Status200OK)
    {
        return new ObjectResult(Envelope.Ok(result))
        {
            StatusCode = statusCode
        };
    }

    public static ActionResult ToCreatedResponse(this object? result, string location)
    {
        return new CreatedResult(location, Envelope.Ok(result));
    }
}