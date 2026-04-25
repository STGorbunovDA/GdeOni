namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;

public sealed record TrackedDeceasedItemResponse(
    Guid Id,
    Guid UserId,
    Guid DeceasedId,
    string RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    bool HasNotificationsEnabled,
    string Status,
    DateTime TrackedAtUtc,
    bool IsActive,
    bool IsMuted,
    bool IsArchived);