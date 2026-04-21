namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;

public sealed record RemoveMemoryCommand(
    Guid DeceasedId,
    Guid MemoryId);