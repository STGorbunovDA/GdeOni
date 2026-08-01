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

    /// <summary>
    /// D47. Относительный путь к «вахтёру» фото — эндпоинту
    /// <c>GET /api/media/{mediaId}/content</c>, который стримит файл только
    /// авторизованному пользователю. Заменяет <see cref="GetPublicUrl"/> для
    /// фото: после закрытия анонимного доступа к бакетам (MinioBootstrap,
    /// publicRead:false) прямые вечные ссылки на MinIO больше не отдаются —
    /// утекший URL без входа отдаёт 401. Путь относительный (от origin,
    /// с префиксом <c>/api</c>) — каждый клиент бьёт по нему своим
    /// авторизованным HTTP-клиентом (web same-origin axios, mobile —
    /// HttpClient с BaseAddress = API). Документы остаются на presigned URL.
    /// </summary>
    string GetPhotoContentPath(Guid mediaId);

    /// <summary>
    /// D36. Базовый URL хранилища без bucket/key — отдаётся клиентам
    /// через <c>/api/app/features</c>, чтобы они сами строили URL
    /// под свою сеть (web → localhost, Android-эмулятор → 10.0.2.2,
    /// production → CDN-домен). Без trailing slash.
    /// </summary>
    string GetMediaBaseUrl();

    Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);

    /// <summary>
    /// F13 (web): стримит файл из storage через сервер. Используется
    /// download-прокси-эндпоинтом, когда presigned URL клиент не может
    /// открыть напрямую (web в Windows не разрешает host
    /// <c>10.0.2.2:9000</c> Android-эмулятора). На production даёт
    /// дополнительную возможность скрыть внутренний хост MinIO и
    /// логировать доступ к файлам.
    /// </summary>
    Task<DownloadedFile> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken);
}

/// <summary>
/// Результат <see cref="IFileStorage.DownloadAsync"/>. Content — поток
/// который caller обязан задиспозить. ContentType приходит из MinIO
/// metadata (записывается при upload). SizeBytes -1 если неизвестен.
/// </summary>
public sealed record DownloadedFile(
    Stream Content,
    string ContentType,
    long SizeBytes);
