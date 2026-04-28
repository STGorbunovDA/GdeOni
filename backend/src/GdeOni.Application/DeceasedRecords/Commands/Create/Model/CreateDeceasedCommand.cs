using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Model;

public sealed record CreateDeceasedCommand(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateTime? BirthDate,
    DateTime DeathDate,
    string? ShortDescription,
    string? Biography,
    CreateDeceasedBurialLocationCommand BurialLocation,
    IReadOnlyCollection<CreateDeceasedPhotoCommand>? Photos,
    IReadOnlyCollection<CreateDeceasedMemoryCommand>? Memories,
    CreateDeceasedMetadataCommand? Metadata);

public sealed record CreateDeceasedBurialLocationCommand(
    double Latitude,
    double Longitude,
    string Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    LocationAccuracy Accuracy);

public sealed record CreateDeceasedPhotoCommand(
    string Url,
    string? Description,
    bool IsPrimary);

public sealed record CreateDeceasedMemoryCommand(
    string Text);

public sealed record CreateDeceasedMetadataCommand(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);
    