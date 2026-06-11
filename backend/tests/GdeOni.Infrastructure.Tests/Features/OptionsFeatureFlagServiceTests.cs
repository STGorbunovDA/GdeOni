using GdeOni.Application.Abstractions.Features;
using GdeOni.Infrastructure.Features;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Tests.Features;

/// <summary>
/// D17. OptionsFeatureFlagService — обёртка над IOptionsMonitor.
/// Проверяем, что значения читаются "на лету" через CurrentValue
/// (важно для hot-reload без рестарта).
/// </summary>
public sealed class OptionsFeatureFlagServiceTests
{
    [Fact]
    public void ReturnsValues_FromOptions()
    {
        var monitor = new TestMonitor(new FeatureFlagsOptions
        {
            SubscriptionEnabled = true,
            GracePeriodDaysAfterExpiry = 3,
        });

        var sut = new OptionsFeatureFlagService(monitor);

        sut.IsSubscriptionEnabled.Should().BeTrue();
        sut.GracePeriodDaysAfterExpiry.Should().Be(3);
    }

    [Fact]
    public void ReturnsDefaults_WhenOptionsNotConfigured()
    {
        var monitor = new TestMonitor(new FeatureFlagsOptions());

        var sut = new OptionsFeatureFlagService(monitor);

        sut.IsSubscriptionEnabled.Should().BeFalse();
        sut.GracePeriodDaysAfterExpiry.Should().Be(0);
    }

    [Fact]
    public void ReflectsHotReload_ThroughCurrentValue()
    {
        var monitor = new TestMonitor(new FeatureFlagsOptions
        {
            SubscriptionEnabled = false,
            GracePeriodDaysAfterExpiry = 0,
        });

        var sut = new OptionsFeatureFlagService(monitor);
        sut.IsSubscriptionEnabled.Should().BeFalse();

        monitor.CurrentValueRef = new FeatureFlagsOptions
        {
            SubscriptionEnabled = true,
            GracePeriodDaysAfterExpiry = 7,
        };

        sut.IsSubscriptionEnabled.Should().BeTrue();
        sut.GracePeriodDaysAfterExpiry.Should().Be(7);
    }

    private sealed class TestMonitor(FeatureFlagsOptions initial) : IOptionsMonitor<FeatureFlagsOptions>
    {
        public FeatureFlagsOptions CurrentValueRef { get; set; } = initial;

        public FeatureFlagsOptions CurrentValue => CurrentValueRef;

        public FeatureFlagsOptions Get(string? name) => CurrentValueRef;

        public IDisposable? OnChange(Action<FeatureFlagsOptions, string?> listener) => null;
    }
}
