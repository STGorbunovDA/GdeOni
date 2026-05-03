using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;

public sealed class MyTrackedDeceasedDetailsResponse
{
    public DeceasedDetailsResponse Deceased { get; init; } = null!;
    public MyTrackingInfoResponse Tracking { get; init; } = null!;
}

public sealed class MyTrackingInfoResponse
{
    public Guid TrackingId { get; init; }
    public string RelationshipType { get; init; } = null!;
    public string? PersonalNotes { get; init; }
    public bool NotifyOnDeathAnniversary { get; init; }
    public bool NotifyOnBirthAnniversary { get; init; }
    public string Status { get; init; } = null!;
    public DateTime TrackedAtUtc { get; init; }
}
