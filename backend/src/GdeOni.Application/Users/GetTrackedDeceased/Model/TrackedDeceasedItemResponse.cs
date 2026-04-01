namespace GdeOni.Application.Users.GetTrackedDeceased.Model;

public sealed class TrackedDeceasedItemResponse
{
    public Guid DeceasedId { get; init; }
    public string RelationshipType { get; init; } = null!;
    public string? PersonalNotes { get; init; }
    public bool NotifyOnDeathAnniversary { get; init; }
    public bool NotifyOnBirthAnniversary { get; init; }
    public string Status { get; init; } = null!;
    public DateTime TrackedAtUtc { get; init; }
}