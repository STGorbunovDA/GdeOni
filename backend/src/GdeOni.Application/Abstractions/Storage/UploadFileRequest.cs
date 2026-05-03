namespace GdeOni.Application.Abstractions.Storage;

public sealed class UploadFileRequest
{
    public required FileKind Kind { get; init; }
    public required Guid DeceasedId { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required Stream Content { get; init; }
}
