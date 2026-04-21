using GdeOni.API.Response;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Extensions;

public sealed class ExceptionMiddleware(
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
            var traceId = context.TraceIdentifier;

            logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);

            if (context.Response.HasStarted)
                throw;

            var error = Error.Failure(
                "server.internal",
                "An unexpected server error occurred.");

            var response = ApiResponse<object?>.Error(error);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}