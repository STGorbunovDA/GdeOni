using GdeOni.Domain.Aggregates.Subscriptions;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// D23. Репозиторий истории платежей. Save() — общая UoW-граница
/// со всеми остальными репозиториями: один <c>AppDbContext</c>,
/// одна транзакция per use-case.
/// </summary>
public interface ISubscriptionPaymentRepository
{
    /// <summary>
    /// Поиск по YooKassa externalPaymentId. Используется webhook'ом
    /// для связывания платежа с юзером (заменяет старый поиск через
    /// <c>User.Subscription.LastPaymentId</c>, см. D23 motivation).
    /// </summary>
    Task<SubscriptionPayment?> GetByExternalPaymentId(string externalPaymentId, CancellationToken cancellationToken);

    /// <summary>
    /// Поиск свежего Pending-платежа юзера (моложе <paramref name="timeout"/>).
    /// Используется <c>CreatePaymentUseCase</c> чтобы при повторном тапе
    /// "Оформить" вернуть существующий CheckoutUrl, а не плодить
    /// новые платежи в YooKassa.
    /// </summary>
    Task<SubscriptionPayment?> GetActivePendingForUser(
        Guid userId,
        TimeSpan timeout,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Пагинированный список платежей для админ-UI с фильтрами.
    /// userId = null означает "все юзеры". emailSearch — частичное
    /// совпадение email (ILIKE %...%), удобнее чем UUID для админа.
    /// </summary>
    Task<(List<(SubscriptionPayment Payment, string UserEmail)> Items, int TotalCount)> GetPagedForAdmin(
        Guid? userId,
        PaymentRecordStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? emailSearch,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// История платежей конкретного юзера (для <c>/me/payments</c>).
    /// Отсортировано по <c>CreatedAtUtc DESC</c>.
    /// </summary>
    Task<(List<SubscriptionPayment> Items, int TotalCount)> GetPagedForUser(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task Add(SubscriptionPayment payment, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}
