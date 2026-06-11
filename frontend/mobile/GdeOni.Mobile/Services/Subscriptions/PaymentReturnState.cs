namespace GdeOni.Mobile.Services.Subscriptions;

/// <summary>
/// E22.7. Статический флаг "юзер только что вернулся с YooKassa-страницы
/// через deep link". MainActivity ставит флаг в OnNewIntent/OnCreate,
/// SubscriptionPage.OnAppearing проверяет и сразу запускает поллинг —
/// без флага юзер бы видел стейл "PendingPayment" до ручного refresh.
///
/// Статика, не DI: deep link приходит в Android-слой ДО того как
/// MAUI shell вообще существует (cold start через intent). DI-контейнер
/// в этот момент может быть не готов. Статический Lock-free флаг
/// проще и надёжнее.
/// </summary>
public static class PaymentReturnState
{
    private static volatile bool _justReturned;

    public static void MarkReturned() => _justReturned = true;

    /// <summary>
    /// Atomically consumes the flag. Возвращает true ровно один раз
    /// после <see cref="MarkReturned"/> — последующие вызовы false
    /// пока не придёт новый deep link.
    /// </summary>
    public static bool ConsumeIfReturned()
    {
        if (!_justReturned) return false;
        _justReturned = false;
        return true;
    }
}
