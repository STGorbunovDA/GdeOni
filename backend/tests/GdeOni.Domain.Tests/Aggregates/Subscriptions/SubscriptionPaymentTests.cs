using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.SubscriptionPayments;

/// <summary>
/// D23. Тесты entity <see cref="SubscriptionPayment"/>:
/// factory + MarkSucceeded/Cancelled/Failed + IsActivePending + idempotency.
/// </summary>
public sealed class SubscriptionPaymentTests
{
    private static readonly Guid SampleUserId = Guid.NewGuid();
    private const string SamplePaymentId = "2cf-000-1111";
    private const string SampleUrl = "https://yoomoney.ru/checkout/abc";
    private static readonly DateTime SampleNow = new(2026, 5, 20, 16, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_HappyPath_ReturnsPaymentInPending()
    {
        var result = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow);

        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        payment.UserId.Should().Be(SampleUserId);
        payment.ExternalPaymentId.Should().Be(SamplePaymentId);
        payment.Plan.Should().Be(SubscriptionPlan.Monthly);
        payment.AmountRub.Should().Be(49m);
        payment.Status.Should().Be(PaymentRecordStatus.Pending);
        payment.CheckoutUrl.Should().Be(SampleUrl);
        payment.CreatedAtUtc.Should().Be(SampleNow);
        payment.UpdatedAtUtc.Should().BeNull();
        payment.PeriodStartUtc.Should().BeNull();
        payment.PeriodEndUtc.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyUserId_ReturnsError()
    {
        var result = SubscriptionPayment.Create(
            Guid.Empty, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("userId.is.required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyExternalId_ReturnsError(string? externalId)
    {
        var result = SubscriptionPayment.Create(
            SampleUserId, externalId!, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.external_id.required");
    }

    [Fact]
    public void Create_NegativeAmount_ReturnsError()
    {
        var result = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, -1m, SampleUrl, SampleNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.amount.invalid");
    }

    [Fact]
    public void Create_WhitespaceCheckoutUrl_NormalizesToNull()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, "   ", SampleNow).Value;

        payment.CheckoutUrl.Should().BeNull();
    }

    [Fact]
    public void MarkSucceeded_FromPending_TransitionsAndClearsCheckoutUrl()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;
        var periodStart = SampleNow.AddMinutes(5);
        var periodEnd = periodStart.AddDays(30);
        var webhookAt = SampleNow.AddMinutes(7);

        var result = payment.MarkSucceeded(periodStart, periodEnd, webhookAt);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentRecordStatus.Succeeded);
        payment.PeriodStartUtc.Should().Be(periodStart);
        payment.PeriodEndUtc.Should().Be(periodEnd);
        payment.UpdatedAtUtc.Should().Be(webhookAt);
        payment.CheckoutUrl.Should().BeNull("checkout URL уже бесполезен после успеха");
    }

    [Fact]
    public void MarkSucceeded_AlreadySucceeded_IsNoOp()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;
        var ps = SampleNow.AddMinutes(5);
        var pe = ps.AddDays(30);
        payment.MarkSucceeded(ps, pe, SampleNow.AddMinutes(7));
        var firstUpdate = payment.UpdatedAtUtc;

        var second = payment.MarkSucceeded(ps, pe, SampleNow.AddDays(1));

        second.IsSuccess.Should().BeTrue();
        payment.UpdatedAtUtc.Should().Be(firstUpdate, "no-op не должен двигать UpdatedAt");
    }

    [Fact]
    public void MarkSucceeded_FromCancelled_ReturnsAlreadyProcessed()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;
        payment.MarkCancelled(SampleNow.AddMinutes(2));

        var result = payment.MarkSucceeded(SampleNow.AddDays(1), SampleNow.AddDays(31), SampleNow.AddDays(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.already_processed");
    }

    [Fact]
    public void MarkSucceeded_PeriodEndBeforeStart_ReturnsError()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;

        var result = payment.MarkSucceeded(SampleNow.AddDays(10), SampleNow.AddDays(5), SampleNow.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.period.invalid");
    }

    [Fact]
    public void MarkCancelled_FromPending_Transitions()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;

        var result = payment.MarkCancelled(SampleNow.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentRecordStatus.Cancelled);
        payment.CheckoutUrl.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_FromPending_Transitions()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;

        var result = payment.MarkFailed(SampleNow.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentRecordStatus.Failed);
        payment.CheckoutUrl.Should().BeNull();
    }

    [Fact]
    public void IsActivePending_FreshPendingWithUrl_True()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;

        payment.IsActivePending(SampleNow.AddMinutes(5), TimeSpan.FromMinutes(10)).Should().BeTrue();
    }

    [Fact]
    public void IsActivePending_ExpiredPending_False()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;

        payment.IsActivePending(SampleNow.AddMinutes(15), TimeSpan.FromMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void IsActivePending_Succeeded_False()
    {
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, SampleUrl, SampleNow).Value;
        payment.MarkSucceeded(SampleNow.AddMinutes(1), SampleNow.AddDays(30), SampleNow.AddMinutes(2));

        payment.IsActivePending(SampleNow.AddMinutes(3), TimeSpan.FromMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void IsActivePending_PendingWithoutUrl_False()
    {
        // Если по какой-то причине CheckoutUrl null (например, наша
        // запись восстановлена из старого состояния) — re-use нечего.
        var payment = SubscriptionPayment.Create(
            SampleUserId, SamplePaymentId, SubscriptionPlan.Monthly, 49m, checkoutUrl: null, SampleNow).Value;

        payment.IsActivePending(SampleNow.AddMinutes(1), TimeSpan.FromMinutes(10)).Should().BeFalse();
    }
}
