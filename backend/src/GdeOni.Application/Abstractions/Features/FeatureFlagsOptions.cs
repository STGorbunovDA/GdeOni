namespace GdeOni.Application.Abstractions.Features;

/// <summary>
/// Глобальные фичефлаги приложения. Биндятся из секции
/// <c>FeatureFlags</c> в appsettings и читаются через
/// <see cref="IFeatureFlagService"/>.
///
/// Решение 2026-05-14: per-feature gating не используется — подписка
/// гейтит всё приложение целиком, остаются только две настройки.
/// </summary>
public sealed class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Включена ли коммерциализация. <c>false</c> — все пользователи
    /// бесплатно (open-beta / preview). <c>true</c> — нужна активная
    /// подписка (или Trial / Admin-роль) для всех authorized
    /// эндпоинтов кроме whitelist (D16).
    /// </summary>
    public bool SubscriptionEnabled { get; set; } = false;

    /// <summary>
    /// Сколько дней после ExpiresAtUtc ещё пускать пользователя на
    /// случай задержки списания или временной недоступности webhook'а
    /// YooKassa. 0 = жёстко по дате.
    /// </summary>
    public int GracePeriodDaysAfterExpiry { get; set; } = 0;
}
