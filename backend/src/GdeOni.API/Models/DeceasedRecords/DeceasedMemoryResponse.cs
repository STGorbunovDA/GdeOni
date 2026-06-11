namespace GdeOni.API.Models.DeceasedRecords;

public sealed class DeceasedMemoryResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = null!;
    public Guid? AuthorUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public string ModerationStatus { get; init; } = null!;
}
