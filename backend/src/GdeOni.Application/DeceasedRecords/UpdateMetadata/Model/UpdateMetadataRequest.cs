namespace GdeOni.Application.DeceasedRecords.UpdateMetadata.Model;

public sealed class UpdateMetadataRequest
{
    public Guid DeceasedId { get; set; }
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}