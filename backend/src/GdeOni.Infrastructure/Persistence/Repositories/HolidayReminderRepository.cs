using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class HolidayReminderRepository(AppDbContext dbContext) : IHolidayReminderRepository
{
    public async Task<IReadOnlyList<HolidayReminder>> GetByUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.HolidayReminders
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<HolidayReminder?> GetByUserAndKey(
        Guid userId,
        string holidayKey,
        CancellationToken cancellationToken)
    {
        return dbContext.HolidayReminders
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.HolidayKey == holidayKey,
                cancellationToken);
    }

    public async Task Add(HolidayReminder reminder, CancellationToken cancellationToken)
    {
        await dbContext.HolidayReminders.AddAsync(reminder, cancellationToken);
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == "23505")
        {
            throw new UniqueConstraintException(postgresException.ConstraintName);
        }
    }
}
