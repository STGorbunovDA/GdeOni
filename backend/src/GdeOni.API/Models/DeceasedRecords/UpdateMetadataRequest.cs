namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос полной замены метаданных карточки умершего владельцем.
/// </summary>
public sealed class UpdateMetadataRequest
{
    /// <summary>Эпитафия на надгробии.</summary>
    public string? Epitaph { get; set; }
    /// <summary>Вероисповедание.</summary>
    public string? Religion { get; set; }
    /// <summary>Источник сведений о захоронении.</summary>
    public string? Source { get; set; }
    /// <summary>Признак участия в военной службе.</summary>
    public bool IsMilitaryService { get; set; }
    /// <summary>Произвольная дополнительная информация.</summary>
    public string? AdditionalInfo { get; set; }
}
