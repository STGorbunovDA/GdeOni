using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;

public sealed record AddDeceasedAtGraveCommand(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography,
    AddDeceasedAtGraveLocationCommand GraveLocation,
    AddDeceasedAtGraveTrackingCommand Tracking);

public sealed record AddDeceasedAtGraveLocationCommand(
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    string? Country,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber);

public sealed record AddDeceasedAtGraveTrackingCommand(
    RelationshipType RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary);
