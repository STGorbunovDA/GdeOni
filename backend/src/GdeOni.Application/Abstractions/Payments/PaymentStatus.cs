namespace GdeOni.Application.Abstractions.Payments;

/// <summary>
/// D16. Унифицированный статус платежа, отдаваемый
/// <see cref="IPaymentProvider"/>. Зеркалит подмножество статусов
/// YooKassa (succeeded / pending / canceled), но не зависит от
/// конкретного провайдера — Fake-провайдер для dev возвращает то же.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Webhook верифицирован, но платёж ещё не завершён (только что
    /// создан, ожидает подтверждения).
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Платёж успешно проведён. Use case активирует подписку.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Платёж отменён (пользователем или провайдером). Use case
    /// возвращает подписку к предыдущему состоянию.
    /// </summary>
    Cancelled = 3,
}
