namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;

public sealed class MyTrackedDeceasedListItemResponse
{
    public Guid TrackingId { get; init; }
    public Guid DeceasedId { get; init; }
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }
    public bool HasGraveLocation { get; init; }
    public double? GraveLatitude { get; init; }
    public double? GraveLongitude { get; init; }

    /// <summary>F17.*. Id главного фото — нужен для редактирования.</summary>
    public Guid? MainMediaId { get; init; }

    /// <summary>
    /// D36. Bucket и storage key главного фото. Клиент сам строит URL
    /// через <c>${mediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>.
    /// Null если фото нет или не Approved.
    /// </summary>
    public string? MainPhotoBucket { get; init; }
    public string? MainPhotoStorageKey { get; init; }

    /// <summary>
    /// DEPRECATED (D36): абсолютный URL хардкодит host из серверного конфига.
    /// Используйте bucket+storageKey. Сохранено для обратной совместимости.
    /// </summary>
    public string? MainPhotoUrl { get; init; }
    public string RelationshipType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public bool NotifyOnDeathAnniversary { get; init; }
    public bool NotifyOnBirthAnniversary { get; init; }
    public DateTime TrackedAtUtc { get; init; }

    /// <summary>
    /// D29. "Проверено" — выставляется админом через PUT /verify.
    /// Юзер видит галочку рядом с именем в списке отслеживаемых,
    /// чтобы быстро понимать какие карточки прошли модерацию.
    /// </summary>
    public bool IsVerified { get; init; }
}
