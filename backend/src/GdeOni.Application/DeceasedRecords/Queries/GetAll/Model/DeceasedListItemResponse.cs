namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

public sealed class GetAllDeceasedItemResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }
    public bool HasBurialLocation { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? AccuracyMeters { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// F17.1. Автор карточки — нужен админ-таблице "все карточки",
    /// чтобы видеть, кто создал запись. Имя резолвится батчем через
    /// IUserRepository.GetDisplayNamesByIds (та же схема что у
    /// воспоминаний F12); если юзер удалён или не найден — null.
    /// </summary>
    public Guid CreatedByUserId { get; init; }
    public string? CreatedByUserName { get; init; }

    /// <summary>
    /// Id главного фото (Approved). Нужен клиенту чтобы потом, если
    /// он откроет редактор, знать какое сейчас выбрано.
    /// </summary>
    public Guid? MainMediaId { get; init; }

    /// <summary>
    /// D36. Bucket + storage key главного фото. Клиент сам строит
    /// URL через `${mediaBaseUrl}/${bucket}/${encodeURIComponent(key)}`,
    /// где mediaBaseUrl приходит из <c>/api/app/features</c> и
    /// различается для web/mobile/iOS. Null если фото нет или не
    /// Approved.
    /// </summary>
    public string? MainPhotoBucket { get; init; }
    public string? MainPhotoStorageKey { get; init; }

    /// <summary>
    /// DEPRECATED (D36): абсолютный URL хардкодит host из серверного
    /// конфига и работает только для одного типа клиента (mobile или
    /// web, не оба сразу). Используйте MainPhotoBucket+MainPhotoStorageKey.
    /// Поле сохранено для обратной совместимости со старыми клиентами;
    /// после выкатки новых клиентов будет удалено.
    /// </summary>
    public string? MainPhotoUrl { get; init; }
}