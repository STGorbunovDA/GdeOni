namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;

public sealed record UpdateMediaDescriptionCommand(
    Guid DeceasedId,
    Guid MediaId,
    string? Description);
