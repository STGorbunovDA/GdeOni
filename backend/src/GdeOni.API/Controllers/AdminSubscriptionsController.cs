using GdeOni.API.Response;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.UseCase;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Model;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Админ-эндпоинты для управления подписками конкретных пользователей.
/// Не путать с самостоятельным cancel'ом подписки юзером — здесь админ
/// моментально снимает подписку независимо от платного периода.
/// </summary>
[ApiController]
[Route("api/admin/users/{userId:guid}/subscription")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AdminSubscriptionsController : ApiControllerBase
{
    /// <summary>
    /// Моментально снять подписку у пользователя userId. Status → Expired,
    /// ExpiresAtUtc → now, доступ закроется при следующем запросе.
    /// Себе снять нельзя (Errors.Subscription.RevokeSelfForbidden).
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid userId,
        [FromServices] IRevokeSubscriptionByAdminUseCase revoke,
        CancellationToken cancellationToken)
    {
        var result = await revoke.Execute(
            new RevokeSubscriptionByAdminCommand(userId),
            cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Перезапустить пробный период подписки. По умолчанию длительность
    /// берётся из SubscriptionOptions (TrialDurationDays=30), при
    /// необходимости можно передать durationDays в теле для отдельного
    /// случая. Работает для любого текущего статуса (Active/Expired/None
    /// и т.д.) — переводит юзера в Trial с новым сроком.
    /// </summary>
    [HttpPost("trial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestartTrial(
        Guid userId,
        [FromBody] RestartTrialRequest? request,
        [FromServices] IRestartTrialByAdminUseCase restart,
        CancellationToken cancellationToken)
    {
        var result = await restart.Execute(
            new RestartTrialByAdminCommand(userId, request?.DurationDays),
            cancellationToken);
        return FromUnitResult(result);
    }
}

public sealed record RestartTrialRequest(int? DurationDays);
