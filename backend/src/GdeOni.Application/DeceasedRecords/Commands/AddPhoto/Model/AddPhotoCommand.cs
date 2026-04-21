namespace GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;

public sealed record AddPhotoCommand(
    Guid DeceasedId,
    string Url,
    string? Description,
    bool IsPrimary,
    Guid AddedByUserId);