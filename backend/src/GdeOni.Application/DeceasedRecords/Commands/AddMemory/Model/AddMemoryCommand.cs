namespace GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;

public sealed record AddMemoryCommand(
    Guid DeceasedId,
    string Text,
    Guid? AuthorUserId);