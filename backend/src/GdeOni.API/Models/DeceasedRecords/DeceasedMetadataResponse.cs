namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Дополнительные метаданные карточки умершего в ответе API.
/// </summary>
public sealed class DeceasedMetadataResponse
{
    /// <summary>Эпитафия на надгробии.</summary>
    public string? Epitaph { get; init; }
    /// <summary>Вероисповедание.</summary>
    public string? Religion { get; init; }
    /// <summary>Источник сведений о захоронении.</summary>
    public string? Source { get; init; }
    /// <summary>Признак участия в военной службе.</summary>
    public bool IsMilitaryService { get; init; }
    /// <summary>Произвольная дополнительная информация.</summary>
    public string? AdditionalInfo { get; init; }
}
