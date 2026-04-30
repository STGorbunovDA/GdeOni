namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;

public sealed record UploadMediaResponse(
    Guid MediaId,
    Guid DeceasedId,
    string Bucket,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    string Url,
    bool IsPresigned);
