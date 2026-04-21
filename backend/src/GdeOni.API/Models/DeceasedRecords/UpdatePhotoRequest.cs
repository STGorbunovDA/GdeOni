namespace GdeOni.API.Models.DeceasedRecords;

public sealed class UpdatePhotoRequest
{
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
}