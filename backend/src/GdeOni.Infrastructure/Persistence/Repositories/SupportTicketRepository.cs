using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketRepository(AppDbContext dbContext) : ISupportTicketRepository
{
    public Task<SupportTicket?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.SupportTickets
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<SupportTicket?> GetByIdWithMessages(Guid id, CancellationToken cancellationToken)
    {
        // Include всей коллекции — в карточке тикета это всегда
        // полная история, постранично грузить смысла нет (десятки
        // сообщений максимум).
        return dbContext.SupportTickets
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(List<(SupportTicket Ticket, string? UserEmail)> Items, int TotalCount)> GetPagedForAdmin(
        Guid? userId,
        SupportTicketStatus[]? statuses,
        SupportTicketSeverity[]? severities,
        SupportTicketKind? kind,
        SupportTicketSource? source,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // LEFT JOIN на users — UserId nullable (для auto-тикетов без
        // привязки к юзеру). DefaultIfEmpty приводит к LEFT JOIN.
        var query = from t in dbContext.SupportTickets.AsNoTracking()
                    join u in dbContext.Users.AsNoTracking()
                        on t.UserId equals u.Id into joinedUsers
                    from u in joinedUsers.DefaultIfEmpty()
                    select new { Ticket = t, Email = (string?)(u != null ? u.Email : null) };

        if (userId.HasValue)
            query = query.Where(x => x.Ticket.UserId == userId.Value);

        if (statuses is { Length: > 0 })
            query = query.Where(x => statuses.Contains(x.Ticket.Status));

        if (severities is { Length: > 0 })
            query = query.Where(x => severities.Contains(x.Ticket.Severity));

        if (kind.HasValue)
            query = query.Where(x => x.Ticket.Kind == kind.Value);

        if (source.HasValue)
            query = query.Where(x => x.Ticket.Source == source.Value);

        if (createdFromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(createdFromUtc.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.Ticket.CreatedAtUtc >= from);
        }

        if (createdToUtc.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(createdToUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(x => x.Ticket.CreatedAtUtc < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            // Ищем в title, description и email юзера. ILIKE кейс-нечувств.
            // Это full-table scan — при росте до десятков тысяч тикетов
            // понадобится pg_trgm + GIN (как для deceased_records, D11.5.2).
            query = query.Where(x =>
                EF.Functions.ILike(x.Ticket.Title, pattern)
                || EF.Functions.ILike(x.Ticket.Description, pattern)
                || (x.Email != null && EF.Functions.ILike(x.Email, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Ticket.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => (x.Ticket, x.Email))
            .ToList();
        return (items!, totalCount);
    }

    public async Task<(List<SupportTicket> Items, int TotalCount)> GetPagedForUser(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SupportTickets
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task Add(SupportTicket ticket, CancellationToken cancellationToken)
    {
        await dbContext.SupportTickets.AddAsync(ticket, cancellationToken);
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            throw new UniqueConstraintException(pg.ConstraintName);
        }
    }
}
