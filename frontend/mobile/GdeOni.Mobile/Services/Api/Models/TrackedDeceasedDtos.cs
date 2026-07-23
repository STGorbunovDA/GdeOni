namespace GdeOni.Mobile.Services.Api.Models;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record TrackedDeceasedListItem(
    Guid TrackingId,
    Guid DeceasedId,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    bool HasGraveLocation,
    double? GraveLatitude,
    double? GraveLongitude,
    // DEPRECATED (D36): абсолютный URL. Используй MainPhotoBucket+StorageKey
    // и IPublicHostsService.BuildMediaUrl. Сохранено для совместимости со
    // старым бэком; будет удалено после миграции.
    string? MainPhotoUrl,
    string RelationshipType,
    string Status,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    DateTime TrackedAtUtc,
    Guid? MainMediaId = null,
    bool IsVerified = false,
    // D36: bucket+storageKey — основной контракт.
    string? MainPhotoBucket = null,
    string? MainPhotoStorageKey = null);
