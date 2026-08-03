using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Events;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>Ручные события пользователя. Стандартный CRUD, per-user.</summary>
public sealed class CustomEventRepository(AppDbContext dbContext) : ICustomEventRepository
{
    public async Task Add(CustomEvent customEvent, CancellationToken cancellationToken)
    {
        await dbContext.CustomEvents.AddAsync(customEvent, cancellationToken);
    }

    public Task<CustomEvent?> GetByIdForUser(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        // Tracked (без AsNoTracking) — потом Update/Delete в той же транзакции.
        return dbContext.CustomEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);
    }

    public Task<List<CustomEvent>> ListForUser(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.CustomEvents.AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public void Delete(CustomEvent customEvent)
    {
        dbContext.CustomEvents.Remove(customEvent);
    }

    public Task Save(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
