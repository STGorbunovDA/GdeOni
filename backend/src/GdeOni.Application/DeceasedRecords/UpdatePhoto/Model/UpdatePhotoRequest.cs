namespace GdeOni.Application.DeceasedRecords.UpdatePhoto.Model;

public sealed class UpdatePhotoRequest
{
    public Guid DeceasedId { get; set; }
    public Guid PhotoId { get; set; }
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
}