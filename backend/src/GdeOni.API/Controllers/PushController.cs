using GdeOni.API.Authorization;
using GdeOni.API.Extensions;
using GdeOni.API.Models.Push;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Подписки браузера на push-уведомления (PWA). Сам пуш шлёт сервер, когда
/// появляется повод (новое обращение, ответ админа, годовщина) — здесь только
/// регистрация и снятие адреса доставки.
///
/// Публичный VAPID-ключ, который нужен браузеру для подписки, отдаётся в
/// <c>GET /api/app/features</c> — там же, где остальной клиентский конфиг.
/// </summary>
[Tags("Push")]
[Route("api/push")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
public sealed class PushController : ApiControllerBase
{
    /// <summary>
    /// Сохранить подписку текущего пользователя. Идемпотентно: повторная
    /// отправка того же endpoint не плодит дубли (иначе одно уведомление
    /// пришло бы на телефон несколько раз).
    /// </summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Subscribe(
        [FromBody] PushSubscriptionRequest request,
        [FromServices] IPushSubscriptionStore store,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        if (string.IsNullOrWhiteSpace(request.Endpoint)
            || string.IsNullOrWhiteSpace(request.P256dh)
            || string.IsNullOrWhiteSpace(request.Auth))
        {
            return BadRequest();
        }

        await store.SaveAsync(
            userIdResult.Value,
            new PushSubscriptionData(request.Endpoint, request.P256dh, request.Auth),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Снять подписку (пользователь выключил уведомления). Идемпотентно:
    /// отсутствие записи — не ошибка.
    /// </summary>
    [HttpDelete("subscriptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] PushUnsubscribeRequest request,
        [FromServices] IPushSubscriptionStore store,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Endpoint))
            await store.RemoveAsync(request.Endpoint, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Включены ли у пользователя push хотя бы на одном устройстве — для
    /// состояния переключателя в профиле.
    /// </summary>
    [HttpGet("subscriptions/status")]
    [ProducesResponseType(typeof(ApiResponse<PushStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(
        [FromServices] IPushSubscriptionStore store,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        var enabled = await store.HasAnyAsync(userIdResult.Value, cancellationToken);
        return new PushStatusResponse(enabled).ToOkResponse();
    }
}
