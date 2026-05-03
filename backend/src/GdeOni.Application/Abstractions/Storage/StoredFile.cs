namespace GdeOni.Application.Abstractions.Storage;

public sealed record StoredFile(
    string Bucket,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string OriginalFileName);
