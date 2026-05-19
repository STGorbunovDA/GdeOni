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
    string? MainPhotoUrl,
    string RelationshipType,
    string Status,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    DateTime TrackedAtUtc);
