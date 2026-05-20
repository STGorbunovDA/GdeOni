using GdeOni.Domain.Aggregates.Subscriptions;

namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;

/// <summary>
/// D23. Запись истории платежей для UI (как mobile, так и admin web).
/// </summary>
public sealed record PaymentRecordResponse(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string ExternalPaymentId,
    string Plan,
    decimal AmountRub,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc)
{
    public static PaymentRecordResponse FromDomain(SubscriptionPayment payment, string? userEmail = null) =>
        new(
            payment.Id,
            payment.UserId,
            userEmail,
            payment.ExternalPaymentId,
            payment.Plan.ToString(),
            payment.AmountRub,
            payment.Status.ToString(),
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.PeriodStartUtc,
            payment.PeriodEndUtc);
}

/// <summary>
/// D23. Пагинированный ответ для history-эндпоинтов.
/// </summary>
public sealed record PagedPaymentsResponse(
    IReadOnlyList<PaymentRecordResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
