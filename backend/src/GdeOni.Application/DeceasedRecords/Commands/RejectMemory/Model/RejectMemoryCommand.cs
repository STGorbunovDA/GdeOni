namespace GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;

public sealed record RejectMemoryCommand(
    Guid DeceasedId,
    Guid MemoryId);