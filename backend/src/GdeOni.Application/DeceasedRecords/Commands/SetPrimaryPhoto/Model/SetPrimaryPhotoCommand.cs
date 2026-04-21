namespace GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;

public sealed record SetPrimaryPhotoCommand(
    Guid DeceasedId,
    Guid PhotoId);