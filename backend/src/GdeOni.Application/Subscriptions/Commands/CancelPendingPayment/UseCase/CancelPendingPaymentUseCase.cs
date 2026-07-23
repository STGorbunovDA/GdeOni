using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.CancelPendingPayment.UseCase;

public sealed class CancelPendingPaymentUseCase(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IPaymentProvider paymentProvider,
    IOptions<SubscriptionOptions> subscriptionOptions,
    TimeProvider timeProvider) : ICancelPendingPaymentUseCase
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
            // Нет свежего Pending — либо синхронизация уже прошла,
            // либо оплата вовсе не начиналась. Считаем успехом:
            // «отмени незавершённое» → нечего отменять.
            return UnitResult.Success<Error>();
        }

        // Сначала пробуем YooKassa. Если провайдер вернул ошибку —
        // не трогаем локальный state: юзер может нажать «Отменить»
        // повторно (сеть восстановится). Идемпотентность в
        // CancelPaymentAsync: уже финализированные платежи → success.
        var cancelResult = await paymentProvider.CancelPaymentAsync(
            pending.ExternalPaymentId, cancellationToken);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        // Локальная запись платежа → Cancelled. MarkCancelled
        // идемпотентен (already-cancelled → success).
        var markResult = pending.MarkCancelled(nowUtc);
        if (markResult.IsFailure)
            return markResult.Error;

        var user = await userRepository.GetById(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        // Откатываем Subscription из PendingPayment в Trial/Expired.
        // Если статус уже не PendingPayment (например, webhook / sync
        // успели подхватить активацию до этого запроса) — вернём
        // NotCancellable, юзер увидит "подписка уже активна".
        var revertResult = user.CancelPendingPayment(nowUtc);
        if (revertResult.IsFailure)
            return revertResult.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
