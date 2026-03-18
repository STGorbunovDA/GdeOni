using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

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
        var normalizedUserName = userName.Trim();

        return dbContext.Users.AnyAsync(
            x => x.UserName == normalizedUserName,
            cancellationToken);
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task Save(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}