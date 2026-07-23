namespace GdeOni.Mobile.Services.Media;

/// <summary>
/// D35. Re-export shared-лимитов. Логика в GdeOni.Mobile.Shared.Media
/// и покрыта тестами; кроссплатформенно используется и вебом.
/// </summary>
internal static class MediaSizeLimits
{
    public const long MaxPhotoSizeBytes = Shared.Media.MediaSizeLimits.MaxPhotoSizeBytes;
    public const long MaxDocumentSizeBytes = Shared.Media.MediaSizeLimits.MaxDocumentSizeBytes;

    public static string? CheckPhoto(long sizeBytes) =>
        Shared.Media.MediaSizeLimits.CheckPhoto(sizeBytes);

    public static string? CheckDocument(long sizeBytes) =>
        Shared.Media.MediaSizeLimits.CheckDocument(sizeBytes);
}
