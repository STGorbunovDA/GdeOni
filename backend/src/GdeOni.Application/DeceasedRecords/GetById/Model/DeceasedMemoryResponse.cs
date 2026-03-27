namespace GdeOni.Application.DeceasedRecords.GetById.Model;

public class DeceasedMemoryResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = null!;
    public string AuthorDisplayName { get; init; } = null!;
    public Guid? AuthorUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int ModerationStatus { get; init; }
}