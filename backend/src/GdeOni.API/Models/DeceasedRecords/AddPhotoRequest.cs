namespace GdeOni.API.Models.DeceasedRecords;

public sealed class AddPhotoRequest
{
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
}