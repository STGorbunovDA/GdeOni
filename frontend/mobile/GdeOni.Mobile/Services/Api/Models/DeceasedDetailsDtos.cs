namespace GdeOni.Mobile.Services.Api.Models;

public sealed record MyTrackedDeceasedDetailsResponse(
    DeceasedDetailsResponse Deceased,
    MyTrackingInfoResponse Tracking);

public sealed record DeceasedDetailsResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    bool HasBurialLocation,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    string? Accuracy,
    string? ShortDescription,
    string? Biography,
    Guid CreatedByUserId,
    bool IsVerified,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DeceasedMetadataResponse Metadata,
    IReadOnlyList<DeceasedMemoryResponse> Memories,
    Guid? MainMediaId = null,
    // D36: bucket+storageKey — основной контракт. Клиент строит URL через
    // IPublicHostsService.BuildMediaUrl(bucket, key).
    string? MainPhotoBucket = null,
    string? MainPhotoStorageKey = null,
    // DEPRECATED (D36): абсолютный URL. Сохранён для обратной совместимости
    // со старыми бэками. После выкатки нового бэка можно удалить.
    string? MainPhotoUrl = null);

public sealed record DeceasedMetadataResponse(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);

public sealed record DeceasedMemoryResponse(
    Guid Id,
    string Text,
    Guid? AuthorUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string ModerationStatus,
    // F12: имя автора (FullName ?? UserName). Null если юзер удалён
    // или старый бэк (без AuthorName) — UI показывает «Аноним».
    string? AuthorName = null);

public sealed record MyTrackingInfoResponse(
    Guid TrackingId,
    string RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    string Status,
    DateTime TrackedAtUtc);

/// <summary>
/// Body для PATCH /api/users/me/tracked-deceased/{id}. Backend требует все
/// поля сразу — это не partial-update, поэтому при изменении одного только
/// статуса (например, при архивации) остальные поля надо взять из текущей
/// карточки и подставить как есть.
/// </summary>
public sealed record UpdateTrackingRequest(
    string RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    string TrackStatus);

public static class TrackStatuses
{
    public const string Active = "Active";
    public const string Muted = "Muted";
    public const string Archived = "Archived";
}
