namespace GdeOni.Mobile.Services.Api.Models;

public sealed record UploadMediaResponse(
    Guid MediaId,
    Guid DeceasedId,
    string Bucket,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    string Url,
    bool IsPresigned);

public sealed record MediaListItem(
    Guid Id,
    Guid DeceasedId,
    Guid UploadedByUserId,
    string Kind,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? Description,
    bool IsMainPhoto,
    string ModerationStatus,
    // Url для документов — presigned (клиент использует как есть).
    // Для фото — public URL, для D36 deprecated; клиент собирает сам
    // из Bucket+StorageKey через PublicHostsService.
    string Url,
    bool IsPresigned,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    // D36: bucket+storageKey для фото. Optional чтобы быть совместимым
    // со старым бэком — он не отдаст эти поля и десериализация даст null.
    string? Bucket = null,
    string? StorageKey = null);

/// <summary>
/// MediaKind в backend — enum int (DeceasedPhoto=1, GravePhoto=2,
/// Document=3, Other=99). Из multipart-формы backend парсит как число
/// — см. MediaIntegrationTests.UploadPhotoAsync.
/// </summary>
public static class MediaKinds
{
    public const int DeceasedPhoto = 1;
    public const int GravePhoto = 2;
    public const int Document = 3;
    public const int Other = 99;

    public const string DeceasedPhotoString = "DeceasedPhoto";
    public const string GravePhotoString = "GravePhoto";
    public const string DocumentString = "Document";
    public const string OtherString = "Other";
}
