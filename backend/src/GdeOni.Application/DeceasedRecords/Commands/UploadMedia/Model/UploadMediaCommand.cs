using GdeOni.Application.Abstractions.Storage;

namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;

public sealed class UploadMediaCommand
{
    public required Guid DeceasedId { get; init; }
    public required FileKind Kind { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required Stream Content { get; init; }
    public string? Description { get; init; }
}
