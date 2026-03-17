using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Deceased;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class DeceasedRepository(AppDbContext dbContext) : IDeceasedRepository
{
    public async Task AddAsync(Deceased deceased, CancellationToken cancellationToken)
    {
        await dbContext.Deceaseds.AddAsync(deceased, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}