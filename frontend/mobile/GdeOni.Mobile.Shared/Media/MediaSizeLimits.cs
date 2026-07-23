namespace GdeOni.Mobile.Shared.Media;

/// <summary>
/// D35. Локальные лимиты на размер media-файлов — зеркало
/// бэкендовских FileValidator.Max*SizeBytes. Без локальной проверки
/// юзер тратит трафик/время на загрузку 15MB фото, получая 400
/// от бэка только после полного аплоада. Также используется на вебе.
/// </summary>
public static class MediaSizeLimits
{
    public const long MaxPhotoSizeBytes = 10L * 1024 * 1024;
    public const long MaxDocumentSizeBytes = 25L * 1024 * 1024;

    public static string? CheckPhoto(long sizeBytes)
    {
        if (sizeBytes > MaxPhotoSizeBytes)
            return "Фото больше 10 МБ — выберите файл поменьше.";
        return null;
    }

    public static string? CheckDocument(long sizeBytes)
    {
        if (sizeBytes > MaxDocumentSizeBytes)
            return "Документ больше 25 МБ — выберите файл поменьше.";
        return null;
    }
}
