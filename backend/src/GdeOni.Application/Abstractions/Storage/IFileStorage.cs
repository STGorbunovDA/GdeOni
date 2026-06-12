namespace GdeOni.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<StoredFile> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// D33. Загрузка в произвольный bucket с явно заданным prefix'ом
    /// в storage key. Используется для support-attachments, где
    /// прикреплённый файл не относится к умершему и MediaKind-based
    /// маршрутизация не подходит.
    /// </summary>
    Task<StoredFile> UploadToBucketAsync(
        string bucket,
        string keyPrefix,
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    string GetPublicUrl(string bucket, string objectKey);

    Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);
}
