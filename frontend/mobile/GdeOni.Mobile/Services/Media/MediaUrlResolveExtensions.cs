using GdeOni.Mobile.Services.Api.Models;

namespace GdeOni.Mobile.Services.Media;

/// <summary>
/// D36. Хелперы для подмены MainPhotoUrl в DTO результатом
/// PublicHostsService.BuildMediaUrlAsync(bucket, key).
///
/// Зачем: бэк после D36 шлёт И bucket+storageKey (новый контракт), И
/// MainPhotoUrl (deprecated). Mobile XAML биндится на MainPhotoUrl —
/// мы перезаписываем его на корректный URL, собранный из bucket+key,
/// и XAML не нужно менять. Так же это правит сценарий "бэк собирает
/// URL с хостом 10.0.2.2, web-клиент его не понимает": каждый клиент
/// строит URL под свой хост-маппинг.
///
/// Если bucket/key пустые (старый бэк) — оставляем item без изменений,
/// сохраняя backward-совместимое поведение со старым MainPhotoUrl.
/// </summary>
public static class MediaUrlResolveExtensions
{
    public static async Task<TrackedDeceasedListItem> ResolveMainPhotoAsync(
        this TrackedDeceasedListItem item,
        IPublicHostsService hosts,
        CancellationToken cancellationToken = default)
    {
        var url = await hosts.BuildMediaUrlAsync(
            item.MainPhotoBucket, item.MainPhotoStorageKey, cancellationToken);
        return url is null ? item : item with { MainPhotoUrl = url };
    }

    public static async Task<DeceasedSearchItem> ResolveMainPhotoAsync(
        this DeceasedSearchItem item,
        IPublicHostsService hosts,
        CancellationToken cancellationToken = default)
    {
        var url = await hosts.BuildMediaUrlAsync(
            item.MainPhotoBucket, item.MainPhotoStorageKey, cancellationToken);
        return url is null ? item : item with { MainPhotoUrl = url };
    }

    public static async Task<NearbyDeceasedItem> ResolveMainPhotoAsync(
        this NearbyDeceasedItem item,
        IPublicHostsService hosts,
        CancellationToken cancellationToken = default)
    {
        var url = await hosts.BuildMediaUrlAsync(
            item.MainPhotoBucket, item.MainPhotoStorageKey, cancellationToken);
        return url is null ? item : item with { MainPhotoUrl = url };
    }

    public static async Task<DeceasedDetailsResponse> ResolveMainPhotoAsync(
        this DeceasedDetailsResponse item,
        IPublicHostsService hosts,
        CancellationToken cancellationToken = default)
    {
        var url = await hosts.BuildMediaUrlAsync(
            item.MainPhotoBucket, item.MainPhotoStorageKey, cancellationToken);
        return url is null ? item : item with { MainPhotoUrl = url };
    }

    /// <summary>
    /// D36 для медиа-галереи. Особенность: документы (Kind == "Document")
    /// отдаются presigned URL'ом — для них bucket+key собрать в публичный
    /// URL нельзя, оставляем как есть. Для фото (DeceasedPhoto / GravePhoto /
    /// Other) — пересобираем через PublicHostsService. Это устраняет
    /// проблему "URL'ы фото с 10.0.2.2 не работают вне Android-эмулятора".
    /// </summary>
    public static async Task<MediaListItem> ResolveUrlAsync(
        this MediaListItem item,
        IPublicHostsService hosts,
        CancellationToken cancellationToken = default)
    {
        if (item.IsPresigned)
            return item;

        var url = await hosts.BuildMediaUrlAsync(
            item.Bucket, item.StorageKey, cancellationToken);
        return url is null ? item : item with { Url = url };
    }
}
