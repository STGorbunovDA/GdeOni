using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions;

/// <summary>
/// D16. Настройки подписки. Биндятся из секции <c>Subscription</c>
/// в appsettings.
/// </summary>
public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// <summary>
    /// Цена месячной подписки в рублях. Решение 2026-05-14: 49 ₽;
    /// пересмотр 2026-07-12: 99 ₽.
    ///
    /// F39: цена отдаётся клиентам через <c>GET /api/app/features</c> —
    /// раньше web и mobile писали «49 ₽/мес» текстом в пяти местах, и
    /// смена цены в конфиге означала бы, что на кнопке одна сумма, а
    /// спишется другая.
    /// </summary>
    public decimal MonthlyPriceRub { get; set; } = 99m;

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
    /// D16. URL, на который YooKassa возвращает мобильного клиента
    /// после оплаты. Deep-link — MAUI-приложение перехватывает и
    /// открывает <c>SubscriptionPage</c> с активным поллингом
    /// (см. <c>SubscriptionViewModel.StartPollingIfPendingAsync</c>).
    /// </summary>
    public string MobileReturnUrl { get; set; } = "gdeoni://payment/return";

    /// <summary>
    /// D16. URL, на который YooKassa возвращает веб-клиента после
    /// оплаты. Страница <c>PaymentReturnPage</c> в React-приложении
    /// поллит <c>/api/users/me/subscription</c> до перехода в Active.
    /// В dev — <c>http://localhost:5173/payment/return</c>; в prod —
    /// публичный HTTPS-URL сайта.
    /// </summary>
    public string WebReturnUrl { get; set; } = "https://gdeoni.ru/payment/return";

    /// <summary>
    /// Возвращает URL возврата для указанного клиента.
    /// </summary>
    public string GetReturnUrl(ClientPlatform platform) => platform switch
    {
        ClientPlatform.Web => WebReturnUrl,
        ClientPlatform.Mobile => MobileReturnUrl,
        _ => MobileReturnUrl,
    };

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
