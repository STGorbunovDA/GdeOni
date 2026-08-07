using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// Внутрисайтовые уведомления. Mark-read делаем через ExecuteUpdate напрямую
/// (минуя Save) — точечные bulk-операции, как RefreshTokenRepository
/// .RevokeAllForUser. Фильтр по recipient гарантирует, что пометить чужое
/// уведомление нельзя.
/// </summary>
public sealed class NotificationRepository(AppDbContext dbContext) : INotificationRepository
{
    public async Task Add(Notification notification, CancellationToken cancellationToken)
    {
        await dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task AddRange(
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken)
    {
        await dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    public Task<List<Notification>> GetRecentForUser(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        return dbContext.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadForUser(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);
    }

    public Task<int> MarkReadForUser(
        Guid notificationId,
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.Notifications
            .Where(n => n.Id == notificationId
                        && n.RecipientUserId == userId
                        && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, nowUtc),
                cancellationToken);
    }

    public Task<int> MarkAllReadForUser(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, nowUtc),
                cancellationToken);
    }

    public Task Save(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
