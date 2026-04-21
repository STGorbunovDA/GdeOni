using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateTracking.Model;

public record UpdateTrackingCommand(
    Guid UserId,
    Guid DeceasedId,
    RelationshipType RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary,
    TrackStatus TrackStatus);