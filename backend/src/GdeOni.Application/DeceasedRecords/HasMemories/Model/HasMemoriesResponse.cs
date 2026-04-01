namespace GdeOni.Application.DeceasedRecords.HasMemories.Model;

public sealed record HasMemoriesResponse(
    Guid DeceasedId,
    bool HasMemories);