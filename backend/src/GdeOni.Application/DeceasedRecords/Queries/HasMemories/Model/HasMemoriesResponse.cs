namespace GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;

public sealed record HasMemoriesResponse(
    Guid DeceasedId,
    bool HasMemories);