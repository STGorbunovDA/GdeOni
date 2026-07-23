using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Extensions;

/// <summary>
/// Хелперы для формирования <see cref="ActionResult"/> из доменных
/// результатов: маппинг ErrorType → HTTP-статус и обёртка успешного
/// результата в <see cref="ApiResponse{T}"/>.
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Единый switch ErrorType→HTTP-статус. Используется и в
    /// ToErrorResponse (через ActionResult), и в ExceptionMiddleware,
    /// который пишет ответ напрямую в Response.StatusCode (D11.9.3).
    /// </summary>
    public static int ToHttpStatusCode(this ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>Оборачивает доменную ошибку в <see cref="ObjectResult"/> с HTTP-статусом по типу ошибки.</summary>
    public static ActionResult ToErrorResponse(this Error error)
    {
        return new ObjectResult(ApiResponse<object?>.Error(error))
        {
            StatusCode = error.Type.ToHttpStatusCode()
        };
    }

    /// <summary>Оборачивает успешный результат в <see cref="ApiResponse{T}"/> с заданным HTTP-статусом (по умолчанию 200).</summary>
    public static ActionResult ToOkResponse<T>(this T result, int statusCode = StatusCodes.Status200OK)
    {
        return new ObjectResult(ApiResponse<T>.Success(result))
        {
            StatusCode = statusCode
        };
    }

    /// <summary>Оборачивает результат в 201 Created с заголовком Location.</summary>
    public static ActionResult ToCreatedResponse<T>(this T result, string location)
    {
        return new CreatedResult(location, ApiResponse<T>.Success(result));
    }
}