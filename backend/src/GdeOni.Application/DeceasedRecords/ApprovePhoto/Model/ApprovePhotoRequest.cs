namespace GdeOni.Application.DeceasedRecords.ApprovePhoto.Model;

public sealed class ApprovePhotoRequest
{
    public Guid DeceasedId { get; set; }
    public Guid PhotoId { get; set; }
}