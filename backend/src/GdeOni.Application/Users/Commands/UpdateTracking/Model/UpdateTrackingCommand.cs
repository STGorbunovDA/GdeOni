using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateTracking.Model;

public record UpdateTrackingCommand(
    Guid DeceasedId,
    RelationshipType RelationshipType,
    string? PersonalNotes,
    IReadOnlyList<int> DeathAnniversaryLeadDays,
    IReadOnlyList<int> BirthAnniversaryLeadDays,
    TrackStatus TrackStatus);