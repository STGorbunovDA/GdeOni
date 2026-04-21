namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;

public sealed record ApproveMemoryCommand(
    Guid DeceasedId,
    Guid MemoryId);