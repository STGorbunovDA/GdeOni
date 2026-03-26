using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class DeceasedRepository(AppDbContext dbContext) : IDeceasedRepository
{
    public async Task Add(Deceased deceased, CancellationToken cancellationToken)
    {
        await dbContext.DeceasedRecords.AddAsync(deceased, cancellationToken);
    }

    public async Task<Deceased?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.DeceasedRecords
            .Include(x => x.Photos)
            .Include(x => x.Memories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Deceased>> GetAll(CancellationToken cancellationToken)
    {
        return await dbContext.DeceasedRecords
            .AsNoTracking()
            .Include(x => x.Photos)
            .Include(x => x.Memories)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken)
    {
        return dbContext.DeceasedRecords
            .AsNoTracking()
            .AnyAsync(x => x.SearchKey == searchKey, cancellationToken);
    }

    public void Delete(Deceased deceased)
    {
        dbContext.DeceasedRecords.Remove(deceased);
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