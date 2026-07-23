namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос правки метаданных карточки умершего от лица редактора
/// (админа/модератора). Полная замена набора метаданных.
/// </summary>
/// <param name="Epitaph">Эпитафия на надгробии.</param>
/// <param name="Religion">Вероисповедание.</param>
/// <param name="Source">Источник сведений о захоронении.</param>
/// <param name="IsMilitaryService">Признак участия в военной службе.</param>
/// <param name="AdditionalInfo">Произвольная дополнительная информация.</param>
public sealed record UpdateMetadataByEditorRequest(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);
