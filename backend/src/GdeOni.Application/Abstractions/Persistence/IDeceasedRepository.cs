using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IDeceasedRepository
{
    Task Add(Deceased deceased, CancellationToken cancellationToken);
    Task<Deceased?> GetById(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdReadOnly(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemories(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemoriesReadOnly(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemoryById(Guid id, Guid memoryId, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMedia(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMediaById(Guid id, Guid mediaId, CancellationToken cancellationToken);
    Task<bool> ExistsById(Guid id, CancellationToken cancellationToken);
    Task<(List<Deceased> Items, int TotalCount)> GetPaged(GetAllDeceasedQuery query, CancellationToken cancellationToken);
    Task<(List<DeceasedMedia> Items, int TotalCount)> GetMediaPaged(
        Guid deceasedId,
        MediaKind? kind,
        ModerationStatus? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken);
    void Delete(Deceased deceased);
    Task Save(CancellationToken cancellationToken);
}