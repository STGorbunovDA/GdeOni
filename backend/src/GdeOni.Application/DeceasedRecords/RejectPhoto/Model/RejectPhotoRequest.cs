namespace GdeOni.Application.DeceasedRecords.RejectPhoto.Model;

public sealed class RejectPhotoRequest
{
    public Guid DeceasedId { get; set; }
    public Guid PhotoId { get; set; }
}