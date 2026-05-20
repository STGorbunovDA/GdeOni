using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Subscriptions;

/// <summary>
/// D23. Запись истории платежей. Один payment в YooKassa = одна
/// запись здесь. Создаётся со <see cref="PaymentRecordStatus.Pending"/>
/// при <c>CreatePayment</c>, переходит в Succeeded / Cancelled / Failed
/// при webhook'е.
///
/// Лежит в отдельной таблице <c>subscription_payments</c>, не
/// owned-collection на <c>User</c>: для админ-отчёта "все платежи
/// за период" нужна сортировка по <c>CreatedAtUtc</c> без загрузки
/// агрегата <c>User</c>.
/// </summary>
public sealed class SubscriptionPayment : Entity<Guid>
{
    public const int MaxExternalIdLength = 64;
    public const int MaxCheckoutUrlLength = 2048;
    public const decimal MinAmountRub = 1m;

    public Guid UserId { get; private set; }
    public string ExternalPaymentId { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public decimal AmountRub { get; private set; }
    public PaymentRecordStatus Status { get; private set; }

    /// <summary>
    /// YooKassa confirmation_url. Заполнен при Pending; обнуляется
    /// при переходе в финальный статус (URL уже бесполезен).
    /// </summary>
    public string? CheckoutUrl { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Границы периода подписки, который оплачивает этот платёж.
    /// Заполняются только при Succeeded (зеркало
    /// <c>User.Subscription.CurrentPeriodStartedAtUtc / ExpiresAtUtc</c>).
    /// </summary>
    public DateTime? PeriodStartUtc { get; private set; }
    public DateTime? PeriodEndUtc { get; private set; }

    private SubscriptionPayment() : base(Guid.Empty)
    {
        ExternalPaymentId = null!;
    }

    private SubscriptionPayment(
        Guid id,
        Guid userId,
        string externalPaymentId,
        SubscriptionPlan plan,
        decimal amountRub,
        string? checkoutUrl,
        DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        ExternalPaymentId = externalPaymentId;
        Plan = plan;
        AmountRub = amountRub;
        Status = PaymentRecordStatus.Pending;
        CheckoutUrl = checkoutUrl;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<SubscriptionPayment, Error> Create(
        Guid userId,
        string externalPaymentId,
        SubscriptionPlan plan,
        decimal amountRub,
        string? checkoutUrl,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
            return Errors.General.ValueIsRequired("userId");

        if (string.IsNullOrWhiteSpace(externalPaymentId))
            return Errors.Payment.ExternalPaymentIdRequired();

        if (externalPaymentId.Length > MaxExternalIdLength)
            return Errors.Payment.ExternalPaymentIdTooLong(MaxExternalIdLength);

        if (!Enum.IsDefined(typeof(SubscriptionPlan), plan))
            return Errors.Subscription.PlanInvalid();

        if (amountRub < MinAmountRub)
            return Errors.Payment.AmountInvalid();

        if (checkoutUrl is { Length: > MaxCheckoutUrlLength })
            return Errors.Payment.CheckoutUrlTooLong(MaxCheckoutUrlLength);

        return new SubscriptionPayment(
            Guid.NewGuid(),
            userId,
            externalPaymentId,
            plan,
            amountRub,
            string.IsNullOrWhiteSpace(checkoutUrl) ? null : checkoutUrl,
            nowUtc);
    }

    /// <summary>
    /// Перевод в Succeeded. Idempotent: повторный вызов на уже
    /// Succeeded → no-op. На Cancelled / Failed возвращает
    /// <see cref="Errors.Payment.AlreadyProcessed"/> — нельзя
    /// "воскресить" платёж, надо создавать новый.
    /// </summary>
    public UnitResult<Error> MarkSucceeded(
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime nowUtc)
    {
        if (Status == PaymentRecordStatus.Succeeded)
            return UnitResult.Success<Error>();

        if (Status != PaymentRecordStatus.Pending)
            return Errors.Payment.AlreadyProcessed();

        if (periodEndUtc <= periodStartUtc)
            return Errors.Payment.PeriodInvalid();

        Status = PaymentRecordStatus.Succeeded;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        CheckoutUrl = null;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkCancelled(DateTime nowUtc)
    {
        if (Status == PaymentRecordStatus.Cancelled)
            return UnitResult.Success<Error>();

        if (Status != PaymentRecordStatus.Pending)
            return Errors.Payment.AlreadyProcessed();

        Status = PaymentRecordStatus.Cancelled;
        CheckoutUrl = null;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkFailed(DateTime nowUtc)
    {
        if (Status == PaymentRecordStatus.Failed)
            return UnitResult.Success<Error>();

        if (Status != PaymentRecordStatus.Pending)
            return Errors.Payment.AlreadyProcessed();

        Status = PaymentRecordStatus.Failed;
        CheckoutUrl = null;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// true если запись всё ещё актуальна для re-use: Pending и
    /// создана не позже <paramref name="timeout"/> назад. Используется
    /// в <c>CreatePaymentUseCase</c> чтобы при многократном тапе
    /// "Оформить" возвращать существующий CheckoutUrl, а не плодить
    /// новые платежи в YooKassa.
    /// </summary>
    public bool IsActivePending(DateTime nowUtc, TimeSpan timeout) =>
        Status == PaymentRecordStatus.Pending
        && CreatedAtUtc + timeout > nowUtc
        && !string.IsNullOrEmpty(CheckoutUrl);
}
