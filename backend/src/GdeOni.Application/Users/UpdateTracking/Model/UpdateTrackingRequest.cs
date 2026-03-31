using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.UpdateTracking.Model;

public sealed class UpdateTrackingRequest
{
    public Guid UserId { get; set; }
    public Guid DeceasedId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string? PersonalNotes { get; set; }
    public bool NotifyOnDeathAnniversary { get; set; }
    public bool NotifyOnBirthAnniversary { get; set; }
}