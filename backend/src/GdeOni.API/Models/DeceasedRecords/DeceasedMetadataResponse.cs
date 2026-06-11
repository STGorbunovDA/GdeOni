namespace GdeOni.API.Models.DeceasedRecords;

public sealed class DeceasedMetadataResponse
{
    public string? Epitaph { get; init; }
    public string? Religion { get; init; }
    public string? Source { get; init; }
    public bool IsMilitaryService { get; init; }
    public string? AdditionalInfo { get; init; }
}
