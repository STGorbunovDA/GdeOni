namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;

public sealed class MediaDetailsResponse
{
    public Guid Id { get; init; }
    public Guid DeceasedId { get; init; }
    public Guid UploadedByUserId { get; init; }
    public string Kind { get; init; } = null!;
    public string OriginalFileName { get; init; } = null!;
    public string Bucket { get; init; } = null!;
    public string StorageKey { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string? Description { get; init; }
    public bool IsMainPhoto { get; init; }
    public string ModerationStatus { get; init; } = null!;
    public string Url { get; init; } = null!;
    public bool IsPresigned { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
