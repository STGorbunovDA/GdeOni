namespace GdeOni.Application.DeceasedRecords.ApproveMemory.Model;

public sealed class ApproveMemoryRequest
{
    public Guid DeceasedId { get; set; }
    public Guid MemoryId { get; set; }
}