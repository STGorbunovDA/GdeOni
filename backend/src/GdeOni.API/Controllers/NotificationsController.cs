using GdeOni.API.Authorization;
using GdeOni.API.Extensions;
using GdeOni.API.Models.Notifications;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// F40. Внутрисайтовые уведомления пользователя («колокольчик» в шапке).
/// Каждый видит только свои (фильтр по recipient на уровне репозитория).
/// Создаются они сервером в фоне (INotificationService из доменных use
/// case'ов) — здесь только чтение и пометка прочитанными.
/// </summary>
[Tags("Notifications")]
[Route("api/notifications")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
public sealed class NotificationsController : ApiControllerBase
{
    private const int MaxLimit = 50;

    /// <summary>Последние уведомления текущего пользователя (новые сверху).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromServices] INotificationRepository repository,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        var take = Math.Clamp(limit, 1, MaxLimit);
        var items = await repository.GetRecentForUser(userIdResult.Value, take, cancellationToken);
        var response = items.Select(NotificationResponse.From).ToList();
        return response.ToOkResponse();
    }

    /// <summary>Сколько непрочитанных — для бейджа на колокольчике.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(
        [FromServices] INotificationRepository repository,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        var count = await repository.CountUnreadForUser(userIdResult.Value, cancellationToken);
        return new UnreadCountResponse(count).ToOkResponse();
    }

    /// <summary>Пометить одно уведомление прочитанным (идемпотентно; только своё).</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkRead(
        Guid id,
        [FromServices] INotificationRepository repository,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        await repository.MarkReadForUser(
            id, userIdResult.Value, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return NoContent();
    }

    /// <summary>Пометить все свои непрочитанные прочитанными.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(
        [FromServices] INotificationRepository repository,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = currentUser.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error.ToErrorResponse();

        await repository.MarkAllReadForUser(
            userIdResult.Value, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return NoContent();
    }
}
