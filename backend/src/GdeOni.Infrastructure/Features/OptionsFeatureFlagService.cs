using GdeOni.Application.Abstractions.Features;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Features;

/// <summary>
/// Обёртка над <see cref="IOptionsMonitor{TOptions}"/>. Reads
/// <see cref="FeatureFlagsOptions"/> на каждом обращении, что даёт
/// hot-reload при правке appsettings без рестарта (IOptionsMonitor
/// сам следит за `IConfiguration` через `reloadOnChange`).
/// Без состояния — регистрируется Singleton.
/// </summary>
public sealed class OptionsFeatureFlagService(
    IOptionsMonitor<FeatureFlagsOptions> options) : IFeatureFlagService
{
    public bool IsSubscriptionEnabled => options.CurrentValue.SubscriptionEnabled;

    public int GracePeriodDaysAfterExpiry => options.CurrentValue.GracePeriodDaysAfterExpiry;
}
