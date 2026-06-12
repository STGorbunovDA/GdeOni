using GdeOni.Domain.Shared;

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

    /// <summary>
    /// D35. Storage-to-storage copy. MinIO server-side copy без
    /// скачивания / повторной загрузки — нужно для "сделать вложение
    /// тикета главным фото умершего": один файл в support-attachments
    /// и в deceased-photos одновременно, дешевле чем copy через
    /// клиента.
    /// </summary>
    Task<StoredFile> CopyObjectAsync(
        string sourceBucket,
        string sourceObjectKey,
        string destBucket,
        string destKeyPrefix,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken);

    /// <summary>
    /// D35. Перегрузка CopyObjectAsync, которая сама выбирает целевой
    /// bucket по MediaKind (тот же резолвер, что и в UploadAsync).
    /// Используется в CopyAttachmentToDeceasedMediaUseCase, чтобы
    /// Application слой не знал имён bucket'ов.
    /// </summary>
    Task<StoredFile> CopyObjectByKindAsync(
        string sourceBucket,
        string sourceObjectKey,
        MediaKind destKind,
        Guid deceasedId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    string GetPublicUrl(string bucket, string objectKey);

    Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);
}
