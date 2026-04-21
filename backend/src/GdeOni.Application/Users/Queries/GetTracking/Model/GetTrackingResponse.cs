namespace GdeOni.Application.Users.Queries.GetTracking.Model;

public sealed class GetTrackingResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid DeceasedId { get; init; }
    public string RelationshipType { get; init; }
    public string? PersonalNotes { get; init; }
    public bool NotifyOnDeathAnniversary { get; init; }
    public bool NotifyOnBirthAnniversary { get; init; }
    public bool HasNotificationsEnabled { get; init; }
    public string Status { get; init; }
    public DateTime TrackedAtUtc { get; init; }
    public bool IsActive { get; init; }
    public bool IsMuted { get; init; }
    public bool IsArchived { get; init; }
}