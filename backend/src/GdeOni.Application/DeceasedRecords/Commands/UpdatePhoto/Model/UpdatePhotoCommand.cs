namespace GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;

public sealed record UpdatePhotoCommand(
    Guid DeceasedId,
    Guid PhotoId,
    string Url,
    string? Description);