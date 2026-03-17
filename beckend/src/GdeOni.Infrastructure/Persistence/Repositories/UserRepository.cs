using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users.AnyAsync(
            x => x.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        var normalizedUserName = userName.Trim();

        return dbContext.Users.AnyAsync(
            x => x.UserName == normalizedUserName,
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}