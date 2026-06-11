using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPaymentRepository(AppDbContext dbContext) : ISubscriptionPaymentRepository
{
    public Task<SubscriptionPayment?> GetByExternalPaymentId(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        return dbContext.SubscriptionPayments
            .FirstOrDefaultAsync(x => x.ExternalPaymentId == externalPaymentId, cancellationToken);
    }

    public Task<SubscriptionPayment?> GetActivePendingForUser(
        Guid userId,
        TimeSpan timeout,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var threshold = nowUtc - timeout;

        // Возвращаем САМУЮ свежую Pending-запись юзера, которая ещё в
        // окне таймаута. CheckoutUrl должен быть непустым — без него
        // re-use бессмыслен.
        return dbContext.SubscriptionPayments
            .Where(x => x.UserId == userId
                && x.Status == PaymentRecordStatus.Pending
                && x.CreatedAtUtc >= threshold
                && x.CheckoutUrl != null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<(SubscriptionPayment Payment, string UserEmail)> Items, int TotalCount)> GetPagedForAdmin(
        Guid? userId,
        PaymentRecordStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? emailSearch,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Join с users для отдачи email в админ-таблице (чтобы не
        // дёргать /users/{id} N раз на UI).
        var query = from p in dbContext.SubscriptionPayments.AsNoTracking()
                    join u in dbContext.Users.AsNoTracking() on p.UserId equals u.Id
                    select new { Payment = p, u.Email };

        if (userId.HasValue)
            query = query.Where(x => x.Payment.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Payment.Status == status.Value);

        if (createdFromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(createdFromUtc.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.Payment.CreatedAtUtc >= from);
        }

        if (createdToUtc.HasValue)
        {
            // Включительно по концу дня: < AddDays(1) of target date.
            var toExclusive = DateTime.SpecifyKind(createdToUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(x => x.Payment.CreatedAtUtc < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(emailSearch))
        {
            var term = emailSearch.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Email, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Payment.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(x => (x.Payment, x.Email)).ToList(), totalCount);
    }

    public async Task<(List<SubscriptionPayment> Items, int TotalCount)> GetPagedForUser(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SubscriptionPayments
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

    public async Task Add(SubscriptionPayment payment, CancellationToken cancellationToken)
    {
        await dbContext.SubscriptionPayments.AddAsync(payment, cancellationToken);
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
