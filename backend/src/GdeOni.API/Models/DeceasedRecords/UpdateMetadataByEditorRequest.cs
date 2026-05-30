namespace GdeOni.API.Models.DeceasedRecords;

public sealed record UpdateMetadataByEditorRequest(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);
