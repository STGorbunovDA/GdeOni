using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
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

    public async Task<(List<Deceased> Items, int TotalCount)> GetPaged(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.DeceasedRecords
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            dbQuery = dbQuery.Where(x =>
                EF.Functions.ILike(x.Name.FirstName, $"%{search}%") ||
                EF.Functions.ILike(x.Name.LastName, $"%{search}%") ||
                (x.Name.MiddleName != null && EF.Functions.ILike(x.Name.MiddleName, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim();
            dbQuery = dbQuery.Where(x => EF.Functions.ILike(x.BurialLocation.Country, country));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            dbQuery = dbQuery.Where(x =>
                x.BurialLocation.City != null &&
                EF.Functions.ILike(x.BurialLocation.City, city));
        }

        if (query.IsVerified.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.IsVerified == query.IsVerified.Value);
        }

        if (query.CreatedFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc <= query.CreatedTo.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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