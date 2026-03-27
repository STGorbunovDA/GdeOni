using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IDeceasedRepository
{
    Task Add(Deceased deceased, CancellationToken cancellationToken);
    Task<Deceased?> GetById(Guid id, CancellationToken cancellationToken);
    Task<(List<Deceased> Items, int TotalCount)> GetPaged(GetAllDeceasedQuery query, CancellationToken cancellationToken);
    Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken);
    void Delete(Deceased deceased);
    Task Save(CancellationToken cancellationToken);
}