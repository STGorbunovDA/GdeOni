using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users.AnyAsync(
            x => x.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken)
    {
        var normalizedUserName = userName.Trim().ToLowerInvariant();

        return dbContext.Users.AnyAsync(
            x => x.UserName == normalizedUserName,
            cancellationToken);
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
}