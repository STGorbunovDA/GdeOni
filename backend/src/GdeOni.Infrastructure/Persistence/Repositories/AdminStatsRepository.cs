using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Admin.Queries.GetAdminStats.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// F38. Счётчики для админской справки.
///
/// Все запросы — агрегатные (COUNT/SUM) и без загрузки сущностей в память.
/// Запросы идут последовательно, а не через Task.WhenAll: один
/// DbContext не потокобезопасен, параллельные запросы на нём падают с
/// «A second operation was started on this context». Страница открывается
/// редко, десяток COUNT'ов по индексам — миллисекунды.
/// </summary>
public sealed class AdminStatsRepository(
    AppDbContext dbContext,
    TimeProvider timeProvider) : IAdminStatsRepository
{
    public async Task<AdminStatsResponse> GetStats(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var users = await GetUsersStats(now, last7Days, last30Days, cancellationToken);
        var deceased = await GetDeceasedStats(last30Days, cancellationToken);
        var content = await GetContentStats(cancellationToken);
        var support = await GetSupportStats(cancellationToken);
        var payments = await GetPaymentsStats(last30Days, cancellationToken);

        return new AdminStatsResponse(users, deceased, content, support, payments, now);
    }

    private async Task<AdminUsersStats> GetUsersStats(
        DateTime now,
        DateTime last7Days,
        DateTime last30Days,
        CancellationToken cancellationToken)
    {
        var users = dbContext.Users.AsNoTracking();

        return new AdminUsersStats(
            Total: await users.CountAsync(cancellationToken),
            NewLast7Days: await users
                .CountAsync(u => u.RegisteredAtUtc >= last7Days, cancellationToken),
            NewLast30Days: await users
                .CountAsync(u => u.RegisteredAtUtc >= last30Days, cancellationToken),
            ActiveLast30Days: await users
                .CountAsync(u => u.LastLoginAtUtc != null && u.LastLoginAtUtc >= last30Days, cancellationToken),
            Admins: await users
                .CountAsync(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin, cancellationToken),
            Blocked: await users
                .CountAsync(u => u.IsBlocked, cancellationToken),
            // Подписка активна = статус Active И срок ещё не вышел. Только
            // статуса мало: Active с истёкшим ExpiresAtUtc — обычное дело,
            // фоновой job'ы, которая переводит его в Expired, у нас нет
            // (см. комментарий в SubscriptionStatus.Expired).
            WithActiveSubscription: await users
                .CountAsync(
                    u => u.Subscription.Status == SubscriptionStatus.Active
                         && u.Subscription.ExpiresAtUtc != null
                         && u.Subscription.ExpiresAtUtc > now,
                    cancellationToken),
            OnTrial: await users
                .CountAsync(
                    u => u.Subscription.Status == SubscriptionStatus.Trial
                         && u.Subscription.ExpiresAtUtc != null
                         && u.Subscription.ExpiresAtUtc > now,
                    cancellationToken),
            WithComplimentaryAccess: await users
                .CountAsync(
                    u => u.ComplimentaryAccessUntilUtc != null
                         && u.ComplimentaryAccessUntilUtc > now,
                    cancellationToken));
    }

    private async Task<AdminDeceasedStats> GetDeceasedStats(
        DateTime last30Days,
        CancellationToken cancellationToken)
    {
        var deceased = dbContext.DeceasedRecords.AsNoTracking();

        return new AdminDeceasedStats(
            Total: await deceased.CountAsync(cancellationToken),
            NewLast30Days: await deceased
                .CountAsync(d => d.CreatedAtUtc >= last30Days, cancellationToken),
            Verified: await deceased
                .CountAsync(d => d.IsVerified, cancellationToken),
            WithCoordinates: await deceased
                .CountAsync(d => d.BurialLocation != null, cancellationToken),
            WithMainPhoto: await deceased
                .CountAsync(d => d.MainMediaId != null, cancellationToken),
            TrackedRecords: await dbContext.Set<TrackedDeceased>()
                .AsNoTracking()
                .CountAsync(cancellationToken));
    }

    private async Task<AdminContentStats> GetContentStats(CancellationToken cancellationToken)
    {
        var media = dbContext.Set<DeceasedMedia>().AsNoTracking();

        return new AdminContentStats(
            Photos: await media
                .CountAsync(m => m.Kind == MediaKind.DeceasedPhoto, cancellationToken),
            GravePhotos: await media
                .CountAsync(m => m.Kind == MediaKind.GravePhoto, cancellationToken),
            Documents: await media
                .CountAsync(m => m.Kind == MediaKind.Document, cancellationToken),
            Memories: await dbContext.Set<DeceasedMemoryEntry>()
                .AsNoTracking()
                .CountAsync(cancellationToken),
            Edits: await dbContext.Set<DeceasedEdit>()
                .AsNoTracking()
                .CountAsync(cancellationToken));
    }

    private async Task<AdminSupportStats> GetSupportStats(CancellationToken cancellationToken)
    {
        var tickets = dbContext.SupportTickets.AsNoTracking();

        return new AdminSupportStats(
            Total: await tickets.CountAsync(cancellationToken),
            Open: await tickets
                .CountAsync(
                    t => t.Status == SupportTicketStatus.Open
                         || t.Status == SupportTicketStatus.InProgress,
                    cancellationToken),
            Resolved: await tickets
                .CountAsync(t => t.Status == SupportTicketStatus.Resolved, cancellationToken));
    }

    private async Task<AdminPaymentsStats> GetPaymentsStats(
        DateTime last30Days,
        CancellationToken cancellationToken)
    {
        var succeeded = dbContext.SubscriptionPayments
            .AsNoTracking()
            .Where(p => p.Status == PaymentRecordStatus.Succeeded);

        return new AdminPaymentsStats(
            SucceededCount: await succeeded.CountAsync(cancellationToken),
            // SumAsync по пустой выборке в EF вернёт 0 (а не null) для
            // decimal — дополнительной обработки не требуется.
            TotalRub: await succeeded.SumAsync(p => p.AmountRub, cancellationToken),
            Last30DaysRub: await succeeded
                .Where(p => p.CreatedAtUtc >= last30Days)
                .SumAsync(p => p.AmountRub, cancellationToken));
    }
}
