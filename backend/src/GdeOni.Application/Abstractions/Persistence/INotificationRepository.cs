using GdeOni.Domain.Aggregates.Notifications;

namespace GdeOni.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task Add(Notification notification, CancellationToken cancellationToken);

    /// <summary>Батч-добавление (фан-аут одного события нескольким админам).</summary>
    Task AddRange(
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken);

    /// <summary>
    /// Последние уведомления пользователя (новые сверху), не более
    /// <paramref name="limit"/>. Для выпадашки «колокольчика».
    /// </summary>
    Task<List<Notification>> GetRecentForUser(
        Guid userId,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>Сколько непрочитанных — для бейджа на колокольчике.</summary>
    Task<int> CountUnreadForUser(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Пометить одно уведомление прочитанным (только своё — фильтр по
    /// recipient). ExecuteUpdate напрямую (минуя Save), как
    /// RefreshTokenRepository.RevokeAllForUser — операция точечная и
    /// самодостаточная. Возвращает число затронутых строк (0 — чужое/нет).
    /// </summary>
    Task<int> MarkReadForUser(
        Guid notificationId,
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    /// <summary>Пометить все непрочитанные пользователя прочитанными (ExecuteUpdate).</summary>
    Task<int> MarkAllReadForUser(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
