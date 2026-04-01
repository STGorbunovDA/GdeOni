using GdeOni.API.Response;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Extensions;

/// <summary>
/// 
/// </summary>
/// <param name="next"></param>
public class ExceptionMiddleware(RequestDelegate next)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var error = Error.Failure(
                "server.internal",
                $"{ex.Message}");

            var response = ApiResponse<object?>.Error(error);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}