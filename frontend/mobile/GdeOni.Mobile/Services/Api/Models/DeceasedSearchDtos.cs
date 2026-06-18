namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// Элемент пейджированного списка от GET /api/deceased-records.
/// После D15 endpoint доступен всем авторизованным (E16 — поиск
/// перед добавлением).
/// </summary>
public sealed record DeceasedSearchItem(
    Guid Id,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    bool HasBurialLocation,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    bool IsVerified,
    DateTime CreatedAtUtc,
    Guid? MainMediaId = null,
    // D36: bucket+storageKey — основной контракт.
    string? MainPhotoBucket = null,
    string? MainPhotoStorageKey = null,
    // DEPRECATED (D36): абсолютный URL, сохранён для совместимости со старым бэком.
    string? MainPhotoUrl = null);

/// <summary>
/// Body для POST /api/users/me/tracked-deceased/{id}. Backend ожидает
/// RelationshipType строкой (D11.15).
/// </summary>
public sealed record AddMeTrackingRequest(
    string RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary);

public sealed record TrackDeceasedResponse(Guid TrackingId, Guid DeceasedId);

/// <summary>
/// Ответ GET /api/users/me/tracked-deceased/{id}/exists.
/// Backend поле: Tracked (bool) — см. IsTrackedByMeResponse в Application.
/// </summary>
public sealed record IsTrackedResponse(bool Tracked);

/// <summary>
/// E21. Элемент пейджированного списка от GET /api/deceased-records/nearby.
/// Координаты всегда есть (карточки без них не попадают в выдачу). Поле
/// DistanceMeters — округлённое расстояние от точки поиска, для UI
/// форматируется через DistanceFormatter ("120 м" / "1.2 км").
/// </summary>
public sealed record NearbyDeceasedItem(
    Guid Id,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    double Latitude,
    double Longitude,
    string? Country,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    bool IsVerified,
    int DistanceMeters,
    Guid? MainMediaId = null,
    // D36: bucket+storageKey — основной контракт.
    string? MainPhotoBucket = null,
    string? MainPhotoStorageKey = null,
    // DEPRECATED (D36): абсолютный URL, сохранён для совместимости со старым бэком.
    string? MainPhotoUrl = null);
