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

    /// <summary>
    /// D36. Bucket и storage key файла. Клиент сам строит публичный URL
    /// для фото (Kind != Document) через
    /// <c>${mediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>.
    /// Для документов (Kind == Document) URL — presigned, его клиент
    /// пересобрать не может; для документов всегда используется
    /// <see cref="Url"/>.
    /// </summary>
    public string Bucket { get; init; } = null!;
    public string StorageKey { get; init; } = null!;

    /// <summary>
    /// Готовый URL. Для документов это presigned (короткоживущий, клиент
    /// обязан использовать его как есть). Для фото — обычный public URL
    /// из MinIO.PublicBaseUrl (deprecated после D36 — клиент собирает сам
    /// через bucket+storageKey).
    /// </summary>
    public string Url { get; init; } = null!;
    public bool IsPresigned { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
