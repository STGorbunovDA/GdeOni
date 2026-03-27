namespace GdeOni.Application.DeceasedRecords.AddPhoto.Model;

public sealed class AddPhotoRequest
{
    public Guid DeceasedId { get; set; }
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
    public Guid AddedByUserId { get; set; }
}