namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;

public sealed record RemovePhotoCommand(
    Guid DeceasedId,
    Guid PhotoId);