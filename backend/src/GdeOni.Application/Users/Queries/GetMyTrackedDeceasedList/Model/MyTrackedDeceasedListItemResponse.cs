namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;

public sealed class MyTrackedDeceasedListItemResponse
{
    public Guid TrackingId { get; init; }
    public Guid DeceasedId { get; init; }
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }
    public bool HasGraveLocation { get; init; }
    public double? GraveLatitude { get; init; }
    public double? GraveLongitude { get; init; }
    public string? MainPhotoUrl { get; init; }
    public string RelationshipType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public bool NotifyOnDeathAnniversary { get; init; }
    public bool NotifyOnBirthAnniversary { get; init; }
    public DateTime TrackedAtUtc { get; init; }
}
