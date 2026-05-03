namespace GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;

public sealed record SetMainMediaPhotoCommand(Guid DeceasedId, Guid MediaId);
