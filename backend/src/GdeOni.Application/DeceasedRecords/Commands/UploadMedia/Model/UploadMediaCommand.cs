using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;

public sealed class UploadMediaCommand
{
    public required Guid DeceasedId { get; init; }
    public required MediaKind Kind { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required Stream Content { get; init; }
    public string? Description { get; init; }
}
