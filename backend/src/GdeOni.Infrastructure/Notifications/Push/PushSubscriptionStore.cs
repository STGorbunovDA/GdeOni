using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Notifications.Push;

/// <summary>
/// Хранилище push-подписок. Собственная граница UoW (SaveChanges внутри):
/// подписка приходит отдельным запросом от клиента и ни с чем не связана.
/// </summary>
public sealed class PushSubscriptionStore(
    AppDbContext dbContext,
    TimeProvider timeProvider) : IPushSubscriptionStore
{
    public async Task SaveAsync(
        Guid userId,
        PushSubscriptionData subscription,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.PushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == subscription.Endpoint, cancellationToken);

        if (existing is not null)
        {
            // Тот же endpoint у другого пользователя — общее устройство или
            // повторный вход под другой учёткой: переназначаем, а не плодим
            // дубли (endpoint уникален).
            existing.UserId = userId;
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
        }
        else
        {
            await dbContext.PushSubscriptions.AddAsync(
                new PushSubscriptionRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Endpoint = subscription.Endpoint,
                    P256dh = subscription.P256dh,
                    Auth = subscription.Auth,
                    CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
                },
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveAsync(string endpoint, CancellationToken cancellationToken)
    {
        return dbContext.PushSubscriptions
            .Where(x => x.Endpoint == endpoint)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<List<PushSubscriptionData>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PushSubscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new PushSubscriptionData(x.Endpoint, x.P256dh, x.Auth))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasAnyAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.PushSubscriptions
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId, cancellationToken);
    }
}
