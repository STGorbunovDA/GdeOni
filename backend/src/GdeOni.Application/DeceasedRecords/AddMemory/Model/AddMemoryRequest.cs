namespace GdeOni.Application.DeceasedRecords.AddMemory.Model;

public sealed class AddMemoryRequest
{
    public Guid DeceasedId { get; set; }
    public string Text { get; set; } = null!;
    public string? AuthorDisplayName { get; set; }
    public Guid? AuthorUserId { get; set; }
}