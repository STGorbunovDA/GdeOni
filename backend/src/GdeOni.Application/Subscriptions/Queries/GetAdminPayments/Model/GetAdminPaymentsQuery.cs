using GdeOni.Domain.Aggregates.Subscriptions;

namespace GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;

/// <summary>
/// D23. Админ-запрос истории платежей. Фильтры: по юзеру, по статусу,
/// по диапазону дат. Все опциональные.
/// </summary>
public sealed record GetAdminPaymentsQuery(
    Guid? UserId = null,
    PaymentRecordStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null,
    int Page = 1,
    int PageSize = 20,
    string? EmailSearch = null);
