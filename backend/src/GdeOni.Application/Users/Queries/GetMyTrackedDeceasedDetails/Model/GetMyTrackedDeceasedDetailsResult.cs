using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;

/// <summary>
/// D36: bucket+storageKey — основной контракт. MainPhotoUrl deprecated,
/// сохранён для обратной совместимости со старыми клиентами.
/// </summary>
public sealed record GetMyTrackedDeceasedDetailsResult(
    Deceased Deceased,
    TrackedDeceased Tracking,
    bool CanSeeAllMemories,
    Guid? MainMediaId,
    string? MainPhotoBucket,
    string? MainPhotoStorageKey,
    string? MainPhotoUrl,
    IReadOnlyDictionary<Guid, string> AuthorNames);
