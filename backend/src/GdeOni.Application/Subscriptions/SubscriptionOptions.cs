namespace GdeOni.Application.Subscriptions;

/// <summary>
/// D16. Настройки подписки. Биндятся из секции <c>Subscription</c>
/// в appsettings.
/// </summary>
public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// <summary>
    /// Цена месячной подписки в рублях. Решение 2026-05-14: 49 ₽.
    /// </summary>
    public decimal MonthlyPriceRub { get; set; } = 49m;

    /// <summary>
    /// Длительность подписки за один платёж. По умолчанию 30 дней
    /// (календарный месяц приближённо).
    /// </summary>
    public int MonthlyDurationDays { get; set; } = 30;

    /// <summary>
    /// Длительность пробного периода. Решение 2026-05-14: 30 дней
    /// при регистрации.
    /// </summary>
    public int TrialDurationDays { get; set; } = 30;

    /// <summary>
    /// Описание товара в платёжном чеке (54-ФЗ).
    /// </summary>
    public string ProductDescription { get; set; } = "Подписка \"Где Они\" — 1 месяц";

    /// <summary>
    /// URL, на который провайдер вернёт пользователя после оплаты.
    /// На mobile — deep-link, на web — наша страница /payment/return.
    /// Конкретное значение из appsettings — например,
    /// "https://gdeoni.ru/payment/return".
    /// </summary>
    public string ReturnUrl { get; set; } = "https://gdeoni.ru/payment/return";

    /// <summary>
    /// Helper: <see cref="TrialDurationDays"/> как TimeSpan.
    /// </summary>
    public TimeSpan TrialDuration => TimeSpan.FromDays(TrialDurationDays);

    /// <summary>
    /// Helper: <see cref="MonthlyDurationDays"/> как TimeSpan.
    /// </summary>
    public TimeSpan MonthlyDuration => TimeSpan.FromDays(MonthlyDurationDays);

    /// <summary>
    /// D23. Сколько Pending-платёж юзера считается актуальным —
    /// в это окно повторный <c>CreatePayment</c> возвращает существующий
    /// CheckoutUrl вместо нового. Зеркало YooKassa
    /// confirmation_url-таймаута (обычно 10 минут).
    /// </summary>
    public int PendingPaymentReuseMinutes { get; set; } = 10;

    public TimeSpan PendingPaymentReuseTimeout =>
        TimeSpan.FromMinutes(PendingPaymentReuseMinutes);
}
