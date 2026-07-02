using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.SyncSubscription.UseCase;

/// <summary>
/// D16. Синхронизация подписки: клиентский pull-запрос "проверь у
/// YooKassa, что там с моим платежом". Работает как:
///
///   1) находим свежий Pending-платёж юзера (тот же критерий, что
///      используется в CreatePayment для дедупликации — «моложе
///      PendingPaymentReuseTimeout»);
///   2) если нет — no-op, ничего синхронизировать не надо;
///   3) если есть — конструируем минимальный webhook-payload с id
///      этого платежа и делегируем всё в
///      <see cref="IProcessPaymentWebhookUseCase"/>: он и pull-запрос
///      к YooKassa сделает (VerifyWebhookAsync лезет в
///      <c>GET /v3/payments/{id}</c>), и применит идемпотентные
///      переходы User.ActivateSubscription/MarkSucceeded.
///
/// Зачем нужно: в dev-окружении localhost не доступен снаружи —
/// YooKassa не может доставить webhook, и подписка вечно висит в
/// PendingPayment. В проде тоже полезно как safety-net: webhook мог
/// потеряться (сетевой сбой, YooKassa задержала retry).
///
/// Идемпотентно: повторный вызов после Active — no-op (webhook
/// use-case делает early-return, если платёж уже Succeeded).
/// </summary>
public sealed class SyncSubscriptionUseCase(
    ICurrentUserService currentUserService,
    ISubscriptionPaymentRepository paymentRepository,
    IProcessPaymentWebhookUseCase processPaymentWebhook,
    IOptions<SubscriptionOptions> subscriptionOptions,
    TimeProvider timeProvider) : ISyncSubscriptionUseCase
{
    public async Task<UnitResult<Error>> Execute(CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var userId = currentUserIdResult.Value;
        var options = subscriptionOptions.Value;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var pending = await paymentRepository.GetActivePendingForUser(
            userId, options.PendingPaymentReuseTimeout, nowUtc, cancellationToken);
        if (pending is null)
        {
            // Нет свежего Pending — либо всё уже финализировано,
            // либо оплата ещё не начиналась. В обоих случаях
            // синхронизировать нечего.
            return UnitResult.Success<Error>();
        }

        // Минимальный webhook payload: VerifyWebhookAsync парсит только
        // object.id и всё остальное подтягивает GET-запросом к
        // YooKassa. Значит других полей класть смысла нет — это тот же
        // самый путь, что и настоящий webhook.
        var payload = $"{{\"object\":{{\"id\":\"{pending.ExternalPaymentId}\"}}}}";
        return await processPaymentWebhook.Execute(
            new ProcessPaymentWebhookCommand(payload, null),
            cancellationToken);
    }
}
