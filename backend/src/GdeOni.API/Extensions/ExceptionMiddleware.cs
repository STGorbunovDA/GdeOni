using GdeOni.API.Response;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Extensions;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred while processing request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var error = Error.Failure(
                "server.internal",
                "An unexpected server error occurred.");

            var response = ApiResponse<object?>.Error(error);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}