namespace GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;

public sealed record DeleteMediaCommand(Guid DeceasedId, Guid MediaId);
