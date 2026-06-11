namespace GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;

/// <summary>
/// D16. Ответ <c>GET /api/users/me/subscription</c>.
/// </summary>
/// <param name="Status">SubscriptionStatus в строковом виде (None/Trial/...).</param>
/// <param name="Plan">SubscriptionPlan если выбран (Monthly), иначе null.</param>
/// <param name="ExpiresAtUtc">Момент окончания текущего периода (Trial или Active).</param>
/// <param name="CancelledAtUtc">Когда отменена (для Cancelled).</param>
/// <param name="IsActiveNow">
/// Учитывает Trial / Active / PendingPayment / Cancelled-paid-period —
/// возвращает то же, что <c>User.HasActiveSubscription</c> без grace
/// (grace применяется только на серверном гейте D16.5). D22:
/// также возвращает true когда у юзера активен complimentary access
/// (даже при отсутствии подписки) — UI должен полагаться на это поле
/// для решения "показывать paywall или нет".
/// </param>
/// <param name="IsOnTrial">Сейчас идёт пробный период (Status=Trial и не истёк).</param>
/// <param name="DaysUntilExpiry">
/// Сколько дней до окончания текущего периода. Округление вверх:
/// день окончания считается оставшимся. 0 если уже истёк или None.
/// </param>
/// <param name="HasComplimentaryAccess">
/// D22. У юзера выдан и активен бесплатный доступ от админа.
/// </param>
/// <param name="ComplimentaryAccessUntilUtc">
/// D22. До какого момента действует complimentary access. null при
/// HasComplimentaryAccess=true означает "бессрочно". null при
/// HasComplimentaryAccess=false — нерелевантно.
/// </param>
/// <param name="ComplimentaryAccessNote">
/// D22. Заметка админа, показывается юзеру в SubscriptionPage
/// ("Причина: {note}"). null = заметки нет.
/// </param>
public sealed record MySubscriptionResponse(
    string Status,
    string? Plan,
    DateTime? ExpiresAtUtc,
    DateTime? CancelledAtUtc,
    bool IsActiveNow,
    bool IsOnTrial,
    int DaysUntilExpiry,
    bool HasComplimentaryAccess,
    DateTime? ComplimentaryAccessUntilUtc,
    string? ComplimentaryAccessNote);
