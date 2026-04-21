namespace GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;

public sealed record ApprovePhotoCommand(
    Guid DeceasedId,
    Guid PhotoId);