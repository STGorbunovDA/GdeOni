namespace GdeOni.API.Models.DeceasedRecords;

public sealed class UpdateMetadataRequest
{
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}