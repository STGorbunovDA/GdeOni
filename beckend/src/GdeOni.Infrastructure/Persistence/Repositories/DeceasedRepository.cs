using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Deceased;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class DeceasedRepository(AppDbContext dbContext) : IDeceasedRepository
{
    public async Task Add(Deceased deceased, CancellationToken cancellationToken)
    {
        await dbContext.Deceaseds.AddAsync(deceased, cancellationToken);
    }

    public Task Save(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}