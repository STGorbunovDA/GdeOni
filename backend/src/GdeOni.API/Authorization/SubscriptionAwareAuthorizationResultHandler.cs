using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace GdeOni.API.Authorization;

/// <summary>
/// D16. Перехватчик результата авторизации. Если запрос завалился
/// именно на <see cref="ActiveSubscriptionRequirement"/> — отдаёт
/// 403 с телом <c>ApiResponse</c>, errorCode=<c>subscription.required</c>.
/// Mobile/web использует этот код, чтобы редиректнуть на paywall
/// (отличить от обычного 403 "нет прав").
///
/// Делегирует дефолтному handler'у для всех остальных кейсов
/// (Forbidden по другим причинам, Challenge и т.п.) — не подменяем
/// общую логику ASP.NET.
/// </summary>
public sealed class SubscriptionAwareAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    /// <summary>
    /// Перехватывает результат авторизации: для падения именно на
    /// <see cref="ActiveSubscriptionRequirement"/> отдаёт 403 с
    /// <c>subscription.required</c>, иначе делегирует дефолтному handler'у.
    /// </summary>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<ActiveSubscriptionRequirement>()
                .Any() == true)
        {
            var error = Errors.Subscription.SubscriptionRequired();
            var response = ApiResponse<object?>.Error(error);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
