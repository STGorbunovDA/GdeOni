namespace GdeOni.Application.DeceasedRecords.RejectMemory.Model;

public sealed class RejectMemoryRequest
{
    public Guid DeceasedId { get; set; }
    public Guid MemoryId { get; set; }
}