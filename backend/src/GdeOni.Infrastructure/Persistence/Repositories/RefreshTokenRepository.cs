using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHash(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task Add(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeAllForUser(Guid userId, CancellationToken cancellationToken)
    {
        // Один SQL UPDATE через ExecuteUpdateAsync — без подгрузки строк
        // и без N round-trip'ов на SaveChanges. Коммитится сразу
        // (auto-transaction), поэтому caller'у не нужен дополнительный Save.
        var nowUtc = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                set => set.SetProperty(t => t.RevokedAtUtc, _ => nowUtc),
                cancellationToken);
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
