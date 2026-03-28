namespace GdeOni.Application.DeceasedRecords.UpdateMemory.Model;

public sealed class UpdateMemoryRequest
{
    public Guid DeceasedId { get; set; }
    public Guid MemoryId { get; set; }
    public string Text { get; set; } = null!;
    public string? AuthorDisplayName { get; set; }
}