using System.Security.Claims;
using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Sentry;

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
        catch (UniqueConstraintException ex)
        {
            // UniqueConstraintException — ожидаемый flow (409 для
            // дубликатов email / user_name / search_key). В Sentry
            // не отправляем — это бизнес-ошибка, не баг.
            logger.LogWarning(
                ex,
                "Unique constraint violation while processing {Method} {Path}. Constraint: {Constraint}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                ex.ConstraintName,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
                throw;

            var error = Errors.UniqueConstraint.FromName(ex.ConstraintName);
            var response = ApiResponse<object?>.Error(error);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            // Берём StatusCode из Error.Type — Errors.UniqueConstraint.FromName
            // сейчас возвращает только Conflict, но в будущем может маппить
            // на NotFound/Validation, и middleware не должен хардкодить 409
            // (см. D11.9.3).
            context.Response.StatusCode = error.Type.ToHttpStatusCode();

            await context.Response.WriteAsJsonAsync(response);
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

            // D21. Захват необработанного исключения в Sentry. Если DSN
            // не сконфигурирован, SentrySdk.IsEnabled=false, и
            // CaptureException — no-op (без сетевых вызовов).
            //
            // ConfigureScope перед Capture: добавляем userId (если
            // юзер аутентифицирован) и traceId для группировки. Email
            // не отправляем — SendDefaultPii=false в SentryRegistration.
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("trace_id", traceId);

                    var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrWhiteSpace(userIdClaim))
                    {
                        scope.User = new SentryUser { Id = userIdClaim };
                    }
                });
                SentrySdk.CaptureException(ex);
            }

            if (context.Response.HasStarted)
                throw;

            var error = Errors.General.InternalServerError();

            var response = ApiResponse<object?>.Error(error);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}