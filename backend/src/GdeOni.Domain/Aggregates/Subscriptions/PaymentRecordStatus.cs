namespace GdeOni.Domain.Aggregates.Subscriptions;

/// <summary>
/// D23. Жизненный цикл записи в <c>subscription_payments</c>. Не путать
/// с <see cref="Domain.Shared.SubscriptionStatus"/> — тот описывает
/// состояние подписки юзера, этот — состояние конкретного платежа.
/// </summary>
public enum PaymentRecordStatus
{
    /// <summary>
    /// Платёж создан в YooKassa, юзер открыл CheckoutUrl, ждём webhook.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Webhook подтвердил оплату, подписка активирована.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Юзер отменил оплату на стороне YooKassa или прошёл таймаут
    /// confirmation_url.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// YooKassa вернула ошибку при создании платежа или платёж был
    /// отклонён банком.
    /// </summary>
    Failed = 3,
}
