namespace GdeOni.Application.Abstractions.Features;

/// <summary>
/// Доступ к глобальным фичефлагам приложения. Реализуется обёрткой
/// над <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>
/// для hot-reload без перезапуска (см.
/// <c>OptionsFeatureFlagService</c> в Infrastructure).
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Включена ли коммерциализация. См. <see cref="FeatureFlagsOptions.SubscriptionEnabled"/>.
    /// </summary>
    bool IsSubscriptionEnabled { get; }

    /// <summary>
    /// Сколько дней grace-периода после ExpiresAtUtc. См.
    /// <see cref="FeatureFlagsOptions.GracePeriodDaysAfterExpiry"/>.
    /// </summary>
    int GracePeriodDaysAfterExpiry { get; }
}
