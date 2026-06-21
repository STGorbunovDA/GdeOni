namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Полный набор данных карточки умершего для отображения на экране
/// деталей: основные поля, место захоронения, метаданные, воспоминания,
/// главное фото.
/// </summary>
public sealed class DeceasedDetailsResponse
{
    /// <summary>Идентификатор карточки умершего.</summary>
    public Guid Id { get; init; }

    /// <summary>Имя умершего.</summary>
    public string FirstName { get; init; } = null!;
    /// <summary>Фамилия умершего.</summary>
    public string LastName { get; init; } = null!;
    /// <summary>Отчество (опционально).</summary>
    public string? MiddleName { get; init; }
    /// <summary>Полное ФИО единой строкой для отображения.</summary>
    public string FullName { get; init; } = null!;

    /// <summary>Дата рождения (опционально).</summary>
    public DateOnly? BirthDate { get; init; }
    /// <summary>Дата смерти.</summary>
    public DateOnly DeathDate { get; init; }

    /// <summary>Признак, что у карточки заданы координаты захоронения.</summary>
    public bool HasBurialLocation { get; init; }
    /// <summary>Широта захоронения.</summary>
    public double? Latitude { get; init; }
    /// <summary>Долгота захоронения.</summary>
    public double? Longitude { get; init; }
    /// <summary>Заявленная точность GPS в метрах.</summary>
    public double? AccuracyMeters { get; init; }
    /// <summary>Страна захоронения.</summary>
    public string? Country { get; init; }
    /// <summary>Регион/область захоронения.</summary>
    public string? Region { get; init; }
    /// <summary>Город или населённый пункт захоронения.</summary>
    public string? City { get; init; }
    /// <summary>Название кладбища.</summary>
    public string? CemeteryName { get; init; }
    /// <summary>Номер участка/сектора.</summary>
    public string? PlotNumber { get; init; }
    /// <summary>Номер могилы.</summary>
    public string? GraveNumber { get; init; }
    // D11.12.1: enum-поле отдаётся строкой (как ModerationStatus,
    // RelationshipType, TrackStatus после D11.1.1). Раньше ехало int 1/2/3/4
    // и ломало симметрию с Create/Update-Request, где Accuracy уже строка.
    /// <summary>Уровень точности координат (строкой из enum LocationAccuracy).</summary>
    public string? Accuracy { get; init; }

    /// <summary>Короткое описание для превью.</summary>
    public string? ShortDescription { get; init; }
    /// <summary>Развёрнутая биография.</summary>
    public string? Biography { get; init; }

    /// <summary>Идентификатор пользователя, создавшего карточку.</summary>
    public Guid CreatedByUserId { get; init; }
    /// <summary>Карточка верифицирована администратором.</summary>
    public bool IsVerified { get; init; }
    /// <summary>Дата и время создания карточки в UTC.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Дата и время последнего изменения карточки в UTC.</summary>
    public DateTime? UpdatedAtUtc { get; init; }

    /// <summary>Дополнительные метаданные карточки.</summary>
    public DeceasedMetadataResponse Metadata { get; init; } = null!;
    /// <summary>Коллекция воспоминаний (memories) на карточке.</summary>
    public IReadOnlyCollection<DeceasedMemoryResponse> Memories { get; init; } = [];

    /// <summary>
    /// Id главного фото (если выбрано и Approved). Нужен для редактирования
    /// (например, в админке отметить какое сейчас главное).
    /// </summary>
    public Guid? MainMediaId { get; init; }

    /// <summary>
    /// D36. Bucket и storage key главного фото. Клиент сам строит
    /// URL через <c>${mediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>,
    /// где mediaBaseUrl приходит из <c>/api/app/features</c>. Null если
    /// фото нет или не Approved.
    /// </summary>
    public string? MainPhotoBucket { get; init; }
    /// <summary>Storage key главного фото в bucket (см. <see cref="MainPhotoBucket"/>).</summary>
    public string? MainPhotoStorageKey { get; init; }

    /// <summary>
    /// DEPRECATED (D36): абсолютный URL хардкодит host из серверного
    /// конфига и работает только для одного типа клиента (mobile или
    /// web, не оба сразу). Используйте MainPhotoBucket+MainPhotoStorageKey.
    /// Поле сохранено для обратной совместимости со старыми клиентами;
    /// будет удалено после выкатки новых клиентов.
    /// </summary>
    public string? MainPhotoUrl { get; init; }
}
