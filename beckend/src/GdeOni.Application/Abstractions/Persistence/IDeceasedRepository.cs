namespace GdeOni.Application.Abstractions.Persistence;

public interface IDeceasedRepository
{
    Task AddAsync(Domain.Aggregates.Deceased.Deceased deceased, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}