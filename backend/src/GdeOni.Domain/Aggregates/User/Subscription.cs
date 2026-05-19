using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

/// <summary>
/// D16. Owned value-object на агрегате <see cref="User"/>. Хранит
/// текущее состояние подписки. Мутации идут только через доменные
/// методы на User (<c>StartTrial</c>, <c>RequestSubscriptionPayment</c>,
/// <c>ActivateSubscription</c>, <c>CancelSubscription</c>), которые
/// создают новый экземпляр через приватные фабрики ниже —
/// VO-инвариант "иммутабельность".
///
/// Конструктор без параметров нужен EF для материализации; полностью
/// заполненный экземпляр строится через <see cref="Initial"/> /
/// <see cref="WithTrial"/> / <see cref="WithPendingPayment"/> /
/// <see cref="WithActive"/> / <see cref="WithCancelled"/>.
/// </summary>
public sealed class Subscription : ValueObject
{
    public const int MaxPaymentIdLength = 100;

    public SubscriptionStatus Status { get; }
    public SubscriptionPlan? Plan { get; }
    public DateTime? CurrentPeriodStartedAtUtc { get; }
    public DateTime? ExpiresAtUtc { get; }
    public string? LastPaymentId { get; }
    public DateTime? CancelledAtUtc { get; }

    private Subscription()
    {
        // EF rehydration. Свойства проставит EF через reflection.
    }

    private Subscription(
        SubscriptionStatus status,
        SubscriptionPlan? plan,
        DateTime? currentPeriodStartedAtUtc,
        DateTime? expiresAtUtc,
        string? lastPaymentId,
        DateTime? cancelledAtUtc)
    {
        Status = status;
        Plan = plan;
        CurrentPeriodStartedAtUtc = currentPeriodStartedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        LastPaymentId = lastPaymentId;
        CancelledAtUtc = cancelledAtUtc;
    }

    /// <summary>
    /// Начальное состояние при создании <see cref="User"/>. До вызова
    /// <c>StartTrial</c> в <c>RegisterUserUseCase</c>.
    /// </summary>
    public static Subscription Initial() => new(
        SubscriptionStatus.None,
        plan: null,
        currentPeriodStartedAtUtc: null,
        expiresAtUtc: null,
        lastPaymentId: null,
        cancelledAtUtc: null);

    internal static Subscription WithTrial(DateTime nowUtc, TimeSpan trialDuration) => new(
        SubscriptionStatus.Trial,
        plan: null,
        currentPeriodStartedAtUtc: nowUtc,
        expiresAtUtc: nowUtc + trialDuration,
        lastPaymentId: null,
        cancelledAtUtc: null);

    internal Subscription WithPendingPayment(SubscriptionPlan plan, string paymentId) => new(
        SubscriptionStatus.PendingPayment,
        plan,
        currentPeriodStartedAtUtc: CurrentPeriodStartedAtUtc,
        expiresAtUtc: ExpiresAtUtc,
        lastPaymentId: paymentId,
        cancelledAtUtc: null);

    internal Subscription WithActive(
        SubscriptionPlan plan,
        DateTime currentPeriodStartedAtUtc,
        DateTime expiresAtUtc,
        string paymentId) => new(
            SubscriptionStatus.Active,
            plan,
            currentPeriodStartedAtUtc,
            expiresAtUtc,
            paymentId,
            cancelledAtUtc: null);

    internal Subscription WithCancelled(DateTime nowUtc) => new(
        SubscriptionStatus.Cancelled,
        Plan,
        CurrentPeriodStartedAtUtc,
        ExpiresAtUtc,
        LastPaymentId,
        cancelledAtUtc: nowUtc);

    /// <summary>
    /// true если у пользователя есть действующий доступ (Trial или
    /// Active с непросроченным ExpiresAtUtc). Cancelled также даёт
    /// доступ до конца paid-period — гейт пускает.
    /// gracePeriodDays добавляется к ExpiresAtUtc, чтобы webhook
    /// YooKassa успел дойти при автосписании.
    /// </summary>
    public bool IsActive(DateTime nowUtc, int gracePeriodDays = 0)
    {
        // PendingPayment включён сознательно: юзер на Trial жмёт
        // "Оплатить" → переход в PendingPayment → ExpiresAtUtc trial
        // ещё в будущем → доступ сохраняется. Юзер с истёкшим Trial,
        // перешедший в PendingPayment, не получит доступ — фильтр по
        // ExpiresAtUtc ниже.
        if (Status is not (SubscriptionStatus.Trial
                or SubscriptionStatus.PendingPayment
                or SubscriptionStatus.Active
                or SubscriptionStatus.Cancelled))
        {
            return false;
        }

        if (ExpiresAtUtc is null)
            return false;

        var effectiveExpiry = ExpiresAtUtc.Value.AddDays(gracePeriodDays);
        return effectiveExpiry > nowUtc;
    }

    /// <summary>
    /// true только если сейчас trial-период (Status=Trial и не истёк).
    /// Используется UI для показа "Пробный период до DD.MM.YYYY".
    /// </summary>
    public bool IsOnTrial(DateTime nowUtc) =>
        Status == SubscriptionStatus.Trial
        && ExpiresAtUtc is { } expires
        && expires > nowUtc;

    /// <summary>
    /// Сколько дней осталось до окончания (Trial / Active / Cancelled).
    /// 0 если уже истекла или None/Expired. Округление вверх — день
    /// окончания считается оставшимся, чтобы UI не показывал "0 дней"
    /// в день списания.
    /// </summary>
    public int DaysUntilExpiry(DateTime nowUtc)
    {
        if (ExpiresAtUtc is not { } expires) return 0;
        if (expires <= nowUtc) return 0;

        var diff = expires - nowUtc;
        return (int)Math.Ceiling(diff.TotalDays);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Status;
        yield return Plan.HasValue;
        yield return Plan ?? default(SubscriptionPlan);
        yield return CurrentPeriodStartedAtUtc.HasValue;
        yield return CurrentPeriodStartedAtUtc ?? default(DateTime);
        yield return ExpiresAtUtc.HasValue;
        yield return ExpiresAtUtc ?? default(DateTime);
        yield return LastPaymentId ?? string.Empty;
        yield return CancelledAtUtc.HasValue;
        yield return CancelledAtUtc ?? default(DateTime);
    }
}
