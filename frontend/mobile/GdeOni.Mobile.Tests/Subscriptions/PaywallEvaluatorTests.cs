using FluentAssertions;
using GdeOni.Mobile.Shared.Subscriptions;
using Xunit;

namespace GdeOni.Mobile.Tests.Subscriptions;

public sealed class PaywallEvaluatorTests
{
    [Fact]
    public void SubscriptionDisabled_NeverShowsPaywall_EvenForExpiredUser()
    {
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: false,
            userRole: "RegularUser",
            isActiveNow: false).Should().BeFalse();
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Admin")]
    [InlineData("superadmin")]   // case-insensitive
    [InlineData("ADMIN")]
    public void Admin_NeverSeesPaywall_EvenWhenSubscriptionEnabledAndInactive(string role)
    {
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: true,
            userRole: role,
            isActiveNow: false).Should().BeFalse();
    }

    [Fact]
    public void RegularUser_ActiveSubscription_NoPaywall()
    {
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: true,
            userRole: "RegularUser",
            isActiveNow: true).Should().BeFalse();
    }

    [Fact]
    public void RegularUser_NoActiveSubscription_ShowsPaywall()
    {
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: true,
            userRole: "RegularUser",
            isActiveNow: false).Should().BeTrue();
    }

    [Fact]
    public void RegularUser_OnTrial_NoPaywall()
    {
        // Trial ⇒ IsActiveNow=true на сервере, так что paywall не нужен.
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: true,
            userRole: "RegularUser",
            isActiveNow: true).Should().BeFalse();
    }

    [Fact]
    public void RegularUser_WithComplimentaryAccess_NoPaywall()
    {
        // Complimentary access делает IsActiveNow=true на сервере (D22).
        PaywallEvaluator.ShouldShowPaywall(
            subscriptionEnabled: true,
            userRole: "RegularUser",
            isActiveNow: true).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UnknownRole")]
    public void UnknownOrNullRole_TreatedAsNonAdmin(string? role)
    {
        // Не Admin/SuperAdmin → обычный путь, paywall зависит от IsActiveNow.
        PaywallEvaluator.ShouldShowPaywall(true, role, isActiveNow: false).Should().BeTrue();
        PaywallEvaluator.ShouldShowPaywall(true, role, isActiveNow: true).Should().BeFalse();
    }
}
