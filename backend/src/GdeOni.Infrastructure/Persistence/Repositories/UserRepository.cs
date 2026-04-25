using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    private IQueryable<User> UsersQuery() =>
        dbContext.Users.Where(x => x.Role != UserRole.SuperAdmin);

    public Task<User?> GetById(Guid userId, CancellationToken cancellationToken)
    {
        return UsersQuery()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdWithTracking(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.TrackedDeceasedItems)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }
    
    public Task<User?> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken)
    {
        return UsersQuery().AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return UsersQuery().AnyAsync(
            x => x.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken)
    {
        var normalizedUserName = userName
            .Trim()
            .ToLowerInvariant();

        return UsersQuery().AnyAsync(
            x => x.UserName == normalizedUserName,
            cancellationToken);
    }
    
    public void Delete(User user)
    {
        dbContext.Users.Remove(user);
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
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

    public async Task<(List<User> Items, int TotalCount)> GetPaged(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = UsersQuery()
            .Include(x => x.TrackedDeceasedItems)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            dbQuery = dbQuery.Where(x =>
                EF.Functions.ILike(x.Email, $"%{search}%") ||
                EF.Functions.ILike(x.UserName, $"%{search}%") ||
                (x.FullName != null && EF.Functions.ILike(x.FullName, $"%{search}%"))
            );
        }

        if (query.Role.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Role == query.Role.Value);
        }

        if (query.RegisteredAtUtc.HasValue)
        {
            var date = DateTime.SpecifyKind(query.RegisteredAtUtc.Value.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            dbQuery = dbQuery.Where(x =>
                x.RegisteredAtUtc >= date &&
                x.RegisteredAtUtc < nextDate);
        }

        if (query.LastLoginAtUtc.HasValue)
        {
            var date = DateTime.SpecifyKind(query.LastLoginAtUtc.Value.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            dbQuery = dbQuery.Where(x =>
                x.LastLoginAtUtc.HasValue &&
                x.LastLoginAtUtc.Value >= date &&
                x.LastLoginAtUtc.Value < nextDate);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.RegisteredAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}