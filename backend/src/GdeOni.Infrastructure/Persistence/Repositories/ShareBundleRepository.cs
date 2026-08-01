using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class ShareBundleRepository(AppDbContext dbContext) : IShareBundleRepository
{
    public async Task Add(ShareBundle bundle, CancellationToken cancellationToken)
    {
        await dbContext.ShareBundles.AddAsync(bundle, cancellationToken);
    }

    public Task<ShareBundle?> GetByCode(string code, CancellationToken cancellationToken)
    {
        return dbContext.ShareBundles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public Task<bool> ExistsByCode(string code, CancellationToken cancellationToken)
    {
        return dbContext.ShareBundles
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, cancellationToken);
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
