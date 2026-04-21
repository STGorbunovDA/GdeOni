namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;

public sealed record UpdateMemoryCommand(
    Guid DeceasedId,
    Guid MemoryId,
    string Text);