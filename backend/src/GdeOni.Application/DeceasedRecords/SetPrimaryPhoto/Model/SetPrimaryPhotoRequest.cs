namespace GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Model;

public sealed class SetPrimaryPhotoRequest
{
    public Guid DeceasedId { get; set; }
    public Guid PhotoId { get; set; }
}