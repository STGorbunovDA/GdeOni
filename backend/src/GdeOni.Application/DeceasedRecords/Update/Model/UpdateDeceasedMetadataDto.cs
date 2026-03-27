namespace GdeOni.Application.DeceasedRecords.Update.Model;

public sealed class UpdateDeceasedMetadataDto
{
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}