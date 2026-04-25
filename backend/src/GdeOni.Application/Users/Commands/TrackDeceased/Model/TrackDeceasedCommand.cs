using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.TrackDeceased.Model;

public record TrackDeceasedCommand(
    Guid DeceasedId,
    RelationshipType RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary);