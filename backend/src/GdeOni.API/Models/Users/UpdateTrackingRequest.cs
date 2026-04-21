using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

public sealed class UpdateTrackingRequest
{
    public RelationshipType RelationshipType { get; set; }
    public string? PersonalNotes { get; set; }
    public bool NotifyOnDeathAnniversary { get; set; }
    public bool NotifyOnBirthAnniversary { get; set; }
    public TrackStatus TrackStatus { get; set; }
}