using GdeOni.API.Authorization;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Subscriptions;
using GdeOni.API.Response;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.Model;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.UseCase;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;
using GdeOni.Application.Subscriptions.Commands.SyncSubscription.UseCase;
using GdeOni.Application.Subscriptions.Commands.CancelPendingPayment.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D16. Управление подпиской пользователя. Все эндпоинты под
/// <c>/api/users/me/subscription</c> — userId берётся из JWT.
///
/// Whitelist для D16.5: контроллер целиком должен быть доступен
/// пользователям без активной подписки (иначе нельзя оформить).
/// Помечается базовым <c>[Authorize]</c>, без
/// <c>RequireActiveSubscription</c>-политики.
/// </summary>
[ApiController]
[Tags("Subscriptions")]
[Route("api/users/me/subscription")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
public sealed class SubscriptionsController : ApiControllerBase
{
    /// <summary>
    /// Текущий статус подписки: Trial / Active / Cancelled / None и т.д.,
    /// плюс DaysUntilExpiry для отображения "осталось N дней".
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<MySubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMy(
        [FromServices] IGetMySubscriptionUseCase getMySubscription,
        CancellationToken cancellationToken)
    {
        var result = await getMySubscription.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Создаёт платёж в YooKassa, помечает пользователя как
    /// <c>PendingPayment</c>. Возвращает <c>CheckoutUrl</c> — клиент
    /// должен открыть его в браузере / WebView для оплаты.
    /// После оплаты YooKassa пришлёт webhook на
    /// <c>POST /api/payments/yookassa/webhook</c> и подписка станет
    /// <c>Active</c>.
    /// </summary>
    [HttpPost("create-payment")]
    [ProducesResponseType(typeof(ApiResponse<CreatePaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        [FromServices] ICreatePaymentUseCase createPayment,
        CancellationToken cancellationToken)
    {
        var result = await createPayment.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отмена подписки. <see cref="GdeOni.Domain.Aggregates.User.Subscription.ExpiresAtUtc"/>
    /// сохраняется — paid-period дорабатывает, автопродления не
    /// будет. После окончания периода гейт перестанет пускать.
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromServices] ICancelSubscriptionUseCase cancelSubscription,
        CancellationToken cancellationToken)
    {
        var result = await cancelSubscription.Execute(
            new CancelSubscriptionCommand(), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Pull-fallback вместо webhook. Клиент дёргает этот эндпоинт
    /// после возврата из YooKassa (или пока статус PendingPayment),
    /// бэк идёт к YooKassa за реальным статусом платежа и, если
    /// платёж succeeded, активирует подписку — та же логика, что
    /// использует webhook-handler.
    ///
    /// Используется когда webhook не доходит: dev-окружение
    /// (localhost не доступен снаружи), сетевой сбой, retry-задержки
    /// у YooKassa. Идемпотентно: повторные вызовы после активации
    /// подписки — no-op.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sync(
        [FromServices] ISyncSubscriptionUseCase syncSubscription,
        CancellationToken cancellationToken)
    {
        var result = await syncSubscription.Execute(cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Отменяет зависший PendingPayment (например, юзер нажал «Назад»
    /// на странице YooKassa и хочет освободиться от Pending): бэк
    /// закрывает checkout-URL у YooKassa, помечает
    /// <c>SubscriptionPayment</c> Cancelled и откатывает подписку в
    /// Trial (если ExpiresAtUtc в будущем) или Expired.
    /// </summary>
    [HttpPost("pending/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPending(
        [FromServices] ICancelPendingPaymentUseCase cancelPending,
        CancellationToken cancellationToken)
    {
        var result = await cancelPending.Execute(cancellationToken);
        return FromUnitResult(result);
    }
}
