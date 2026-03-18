namespace GdeOni.Application.Abstractions.Persistence;

public interface IDeceasedRepository
{
    Task Add(Domain.Aggregates.Deceased.Deceased deceased, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}