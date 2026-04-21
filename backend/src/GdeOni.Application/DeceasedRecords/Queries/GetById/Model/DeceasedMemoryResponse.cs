namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

public sealed class DeceasedMemoryResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = null!;
    public Guid? AuthorUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int ModerationStatus { get; init; }
}