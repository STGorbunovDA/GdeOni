using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// D16. Тесты доменной логики подписки на агрегате <see cref="User"/>.
/// Покрываем: StartTrial idempotency, HasActiveSubscription для всех
/// статусов, ActivateSubscription/Cancel/RequestPayment, IsOnTrial,
/// DaysUntilExpiry.
/// </summary>
public sealed class SubscriptionTests
{
    private static readonly DateTime BaseNowUtc = new(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
    private const string SampleEmail = "ivan@example.com";
    private const string SamplePasswordHash = "hash$with$enough$chars";

    /// <summary>
    /// Свежезарегистрированный юзер сразу после Register — Status=None,
    /// HasActiveSubscription=false. Trial стартует use-case'ом, домен
    /// сам ничего не выставляет в Register.
    /// </summary>
    [Fact]
    public void Register_FreshUser_HasNoneSubscription()
    {
        var user = CreateSampleUser();

        user.Subscription.Status.Should().Be(SubscriptionStatus.None);
        user.Subscription.Plan.Should().BeNull();
        user.Subscription.ExpiresAtUtc.Should().BeNull();
        user.HasActiveSubscription(BaseNowUtc).Should().BeFalse();
        user.IsOnTrial(BaseNowUtc).Should().BeFalse();
    }

    /// <summary>
    /// StartTrial из None переводит в Trial + выставляет ExpiresAtUtc =
    /// nowUtc + duration. После этого HasActiveSubscription и IsOnTrial
    /// дают true.
    /// </summary>
    [Fact]
    public void StartTrial_FromNone_SetsTrialAndExpiresAt()
    {
        var user = CreateSampleUser();

        var result = user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Trial);
        user.Subscription.ExpiresAtUtc.Should().Be(BaseNowUtc.AddDays(30));
        user.Subscription.CurrentPeriodStartedAtUtc.Should().Be(BaseNowUtc);
        user.HasActiveSubscription(BaseNowUtc).Should().BeTrue();
        user.IsOnTrial(BaseNowUtc).Should().BeTrue();
    }

    /// <summary>
    /// Повторный StartTrial — no-op. Защита от двойного вызова в
    /// миграционных сценариях. ExpiresAtUtc не меняется.
    /// </summary>
    [Fact]
    public void StartTrial_TwiceOnNotNone_IsNoOp()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));
        var originalExpires = user.Subscription.ExpiresAtUtc;

        var laterNow = BaseNowUtc.AddDays(5);
        var result = user.StartTrial(laterNow, TimeSpan.FromDays(60));

        result.IsSuccess.Should().BeTrue();
        user.Subscription.ExpiresAtUtc.Should().Be(originalExpires);
    }

    /// <summary>
    /// trialDuration ≤ 0 — Validation error.
    /// </summary>
    [Fact]
    public void StartTrial_ZeroDuration_ReturnsValidationError()
    {
        var user = CreateSampleUser();

        var result = user.StartTrial(BaseNowUtc, TimeSpan.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Trial истёк — HasActiveSubscription/IsOnTrial возвращают false.
    /// </summary>
    [Fact]
    public void HasActiveSubscription_AfterTrialExpired_ReturnsFalse()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));
        var afterExpiry = BaseNowUtc.AddDays(31);

        user.HasActiveSubscription(afterExpiry).Should().BeFalse();
        user.IsOnTrial(afterExpiry).Should().BeFalse();
    }

    /// <summary>
    /// GracePeriodDays продлевает действие подписки за ExpiresAtUtc.
    /// Юзер с истёкшим trial через 1 день после ExpiresAtUtc должен
    /// пускаться при gracePeriodDays >= 1.
    /// </summary>
    [Fact]
    public void HasActiveSubscription_WithGracePeriod_PassesAfterExpiry()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));
        var oneDayAfterExpiry = BaseNowUtc.AddDays(30).AddHours(12);

        user.HasActiveSubscription(oneDayAfterExpiry, gracePeriodDays: 0).Should().BeFalse();
        user.HasActiveSubscription(oneDayAfterExpiry, gracePeriodDays: 2).Should().BeTrue();
    }

    /// <summary>
    /// RequestSubscriptionPayment из Trial переводит в PendingPayment,
    /// сохраняет paymentId и Plan. ExpiresAtUtc trial-периода не
    /// сбрасывается — юзер продолжает иметь доступ.
    /// </summary>
    [Fact]
    public void RequestSubscriptionPayment_FromTrial_SwitchesToPending()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));

        var result = user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-abc-123");

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        user.Subscription.Plan.Should().Be(SubscriptionPlan.Monthly);
        user.Subscription.LastPaymentId.Should().Be("pay-abc-123");
        // Trial-доступ сохраняется до конца pending — иначе юзер видит
        // 403 пока YooKassa-webhook докатится.
        user.HasActiveSubscription(BaseNowUtc).Should().BeTrue();
    }

    /// <summary>
    /// RequestSubscriptionPayment с Active = AlreadyActive (защита от
    /// двойной оплаты).
    /// </summary>
    [Fact]
    public void RequestSubscriptionPayment_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        var user = CreateSampleUser();
        ActivateUserSubscription(user, "pay-1");

        var result = user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-2");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.already.active");
    }

    /// <summary>
    /// ActivateSubscription из PendingPayment → Active, ExpiresAtUtc
    /// выставлен в будущее, HasActiveSubscription=true, IsOnTrial=false.
    /// </summary>
    [Fact]
    public void ActivateSubscription_FromPending_ActivatesAndExtendsExpiry()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-1");

        var paidExpires = BaseNowUtc.AddDays(30 + 30); // +30 trial +30 paid
        var result = user.ActivateSubscription(
            SubscriptionPlan.Monthly, BaseNowUtc, paidExpires, "pay-1");

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Active);
        user.Subscription.ExpiresAtUtc.Should().Be(paidExpires);
        user.HasActiveSubscription(BaseNowUtc).Should().BeTrue();
        user.IsOnTrial(BaseNowUtc).Should().BeFalse();
    }

    /// <summary>
    /// Повторный webhook с тем же paymentId и тем же expiresAt → no-op
    /// (idempotency). ExpiresAtUtc не двигается.
    /// </summary>
    [Fact]
    public void ActivateSubscription_RetryWithSamePayment_IsIdempotent()
    {
        var user = CreateSampleUser();
        var firstExpires = BaseNowUtc.AddDays(30);
        user.ActivateSubscription(SubscriptionPlan.Monthly, BaseNowUtc, firstExpires, "pay-1");

        var result = user.ActivateSubscription(SubscriptionPlan.Monthly, BaseNowUtc, firstExpires, "pay-1");

        result.IsSuccess.Should().BeTrue();
        user.Subscription.ExpiresAtUtc.Should().Be(firstExpires);
    }

    /// <summary>
    /// Activation с ExpiresAtUtc &lt;= nowUtc → Validation error.
    /// </summary>
    [Fact]
    public void ActivateSubscription_ExpiresInPast_ReturnsError()
    {
        var user = CreateSampleUser();

        var result = user.ActivateSubscription(
            SubscriptionPlan.Monthly, BaseNowUtc, BaseNowUtc.AddDays(-1), "pay-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// CancelSubscription из Active → Status=Cancelled, ExpiresAtUtc
    /// сохраняется, HasActiveSubscription пока true (paid-period
    /// дорабатывает).
    /// </summary>
    [Fact]
    public void CancelSubscription_FromActive_PreservesPaidPeriod()
    {
        var user = CreateSampleUser();
        var paidExpires = BaseNowUtc.AddDays(30);
        ActivateUserSubscription(user, "pay-1", paidExpires);

        var result = user.CancelSubscription(BaseNowUtc);

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        user.Subscription.ExpiresAtUtc.Should().Be(paidExpires);
        user.Subscription.CancelledAtUtc.Should().Be(BaseNowUtc);
        user.HasActiveSubscription(BaseNowUtc).Should().BeTrue();
    }

    /// <summary>
    /// Cancelled с истёкшим ExpiresAtUtc → доступ закрыт.
    /// </summary>
    [Fact]
    public void HasActiveSubscription_CancelledExpired_ReturnsFalse()
    {
        var user = CreateSampleUser();
        ActivateUserSubscription(user, "pay-1", BaseNowUtc.AddDays(30));
        user.CancelSubscription(BaseNowUtc);

        var afterPeriod = BaseNowUtc.AddDays(31);

        user.HasActiveSubscription(afterPeriod).Should().BeFalse();
    }

    /// <summary>
    /// CancelSubscription из None/Expired/PendingPayment — NotCancellable.
    /// </summary>
    [Fact]
    public void CancelSubscription_FromNone_ReturnsNotCancellable()
    {
        var user = CreateSampleUser();

        var result = user.CancelSubscription(BaseNowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.not_cancellable");
    }

    /// <summary>
    /// DaysUntilExpiry — округление вверх. День окончания считается
    /// оставшимся (UI не должен показывать "0 дней" в день списания).
    /// </summary>
    [Fact]
    public void DaysUntilExpiry_RoundsUp()
    {
        var user = CreateSampleUser();
        user.StartTrial(BaseNowUtc, TimeSpan.FromDays(30));

        user.Subscription.DaysUntilExpiry(BaseNowUtc).Should().Be(30);
        user.Subscription.DaysUntilExpiry(BaseNowUtc.AddDays(29).AddHours(1)).Should().Be(1);
        user.Subscription.DaysUntilExpiry(BaseNowUtc.AddDays(30)).Should().Be(0);
        user.Subscription.DaysUntilExpiry(BaseNowUtc.AddDays(31)).Should().Be(0);
    }

    private static User CreateSampleUser()
    {
        var result = User.Register(SampleEmail, SamplePasswordHash);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static void ActivateUserSubscription(
        User user,
        string paymentId,
        DateTime? expiresAtUtc = null)
    {
        var expires = expiresAtUtc ?? BaseNowUtc.AddDays(30);
        user.ActivateSubscription(SubscriptionPlan.Monthly, BaseNowUtc, expires, paymentId);
    }
}
