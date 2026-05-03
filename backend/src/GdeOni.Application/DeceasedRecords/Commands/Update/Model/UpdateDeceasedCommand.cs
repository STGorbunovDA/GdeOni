using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.Model;

public sealed record UpdateDeceasedCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography,
    UpdateDeceasedBurialLocationCommand? BurialLocation,
    UpdateDeceasedMetadataCommand? Metadata);

public sealed record UpdateDeceasedBurialLocationCommand(
    double Latitude,
    double Longitude,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    LocationAccuracy Accuracy,
    double? AccuracyMeters);

public sealed record UpdateDeceasedMetadataCommand(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);