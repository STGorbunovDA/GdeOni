namespace GdeOni.Mobile.Shared.Subscriptions;

/// <summary>
/// E22.6. Чистая логика "показывать ли paywall конкретному юзеру".
/// Без зависимостей от Refit/MAUI — легко тестируется. Зеркало
/// серверного <c>ActiveSubscriptionAuthorizationHandler</c> (D16.5),
/// чтобы UI не дёргал гейт зря.
///
/// Решение OR:
///   - SubscriptionEnabled=false → не показывать (open-beta);
///   - admin (SuperAdmin/Admin) → не показывать (бэк всё равно пускает);
///   - IsActiveNow=true (включая complimentary, D22) → не показывать;
///   - иначе → показывать.
/// </summary>
public static class PaywallEvaluator
{
    /// <summary>
    /// Возвращает true, если нужно завернуть юзера на
    /// SubscriptionRequiredPage.
    /// </summary>
    /// <param name="subscriptionEnabled">
    /// <c>AppFeaturesResponse.SubscriptionEnabled</c>.
    /// </param>
    /// <param name="userRole">
    /// Серверная роль юзера в строковом виде ("SuperAdmin"/"Admin"/...).
    /// </param>
    /// <param name="isActiveNow">
    /// <c>MySubscriptionResponse.IsActiveNow</c> — учитывает Trial /
    /// Active / Cancelled-paid-period / Complimentary (D22).
    /// </param>
    public static bool ShouldShowPaywall(
        bool subscriptionEnabled,
        string? userRole,
        bool isActiveNow)
    {
        if (!subscriptionEnabled)
            return false;

        if (IsAdmin(userRole))
            return false;

        return !isActiveNow;
    }

    private static bool IsAdmin(string? role) =>
        string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
}
