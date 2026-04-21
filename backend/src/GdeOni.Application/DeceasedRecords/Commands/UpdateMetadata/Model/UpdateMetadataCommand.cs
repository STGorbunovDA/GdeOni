namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;

public sealed record UpdateMetadataCommand(
    Guid DeceasedId,
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);