using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IDeceasedRepository
{
    Task Add(Deceased deceased, CancellationToken cancellationToken);
    Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}