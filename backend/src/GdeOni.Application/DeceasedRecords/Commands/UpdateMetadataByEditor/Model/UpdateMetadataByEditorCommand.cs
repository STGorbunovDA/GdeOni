namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Model;

/// <summary>
/// D24. PATCH /api/deceased/{id}/metadata — обновление метаданных
/// (эпитафия, религия, источник, военная служба, доп. инфо) трекающим
/// юзером или админом.
/// </summary>
public sealed record UpdateMetadataByEditorCommand(
    Guid DeceasedId,
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);
