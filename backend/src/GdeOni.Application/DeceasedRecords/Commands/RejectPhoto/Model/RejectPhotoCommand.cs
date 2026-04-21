namespace GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;

public sealed record RejectPhotoCommand(
    Guid DeceasedId,
    Guid PhotoId);