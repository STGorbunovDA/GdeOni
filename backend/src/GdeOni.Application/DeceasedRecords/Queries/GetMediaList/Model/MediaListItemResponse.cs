namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;

public sealed class MediaListItemResponse
{
    public Guid Id { get; init; }
    public Guid DeceasedId { get; init; }
    public Guid UploadedByUserId { get; init; }
    public string Kind { get; init; } = null!;
    public string OriginalFileName { get; init; } = null!;
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
