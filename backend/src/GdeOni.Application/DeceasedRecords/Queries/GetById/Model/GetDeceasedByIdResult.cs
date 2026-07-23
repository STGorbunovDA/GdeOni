using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

/// <summary>
/// MainMediaId / MainPhotoBucket / MainPhotoStorageKey / MainPhotoUrl кладёт
/// сюда use case (отдельным batched-запросом за главным фото) — Domain-
/// агрегат при ReadOnly-загрузке не имеет заinclude'ленной коллекции Media,
/// поэтому Deceased.GetMainPhoto() вернул бы null даже когда фото есть.
/// Это лекарство от N+1: клиент получает идентификацию главного фото
/// в том же ответе, без второго /media-запроса.
///
/// D36: bucket+storageKey — основной контракт. MainPhotoUrl deprecated,
/// сохранён для обратной совместимости.
/// </summary>
public sealed record GetDeceasedByIdResult(
    Deceased Deceased,
    bool CanSeeAllMemories,
    Guid? MainMediaId,
    string? MainPhotoBucket,
    string? MainPhotoStorageKey,
    string? MainPhotoUrl,
    IReadOnlyDictionary<Guid, string> AuthorNames);
