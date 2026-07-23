using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос создания новой карточки умершего: основные поля, опционально
/// место захоронения, начальные воспоминания и метаданные.
/// </summary>
public sealed class CreateDeceasedRequest
{
    /// <summary>Имя умершего.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Фамилия умершего.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Отчество (необязательно).</summary>
    public string? MiddleName { get; set; }

    /// <summary>Дата рождения (необязательно).</summary>
    public DateOnly? BirthDate { get; set; }
    /// <summary>Дата смерти.</summary>
    public DateOnly DeathDate { get; set; }

    /// <summary>Короткое описание для превью карточки.</summary>
    public string? ShortDescription { get; set; }
    /// <summary>Развёрнутая биография.</summary>
    public string? Biography { get; set; }

    /// <summary>Опциональные координаты и адресные атрибуты захоронения.</summary>
    public CreateDeceasedBurialLocationRequest? BurialLocation { get; set; }
    /// <summary>Опциональные начальные воспоминания (memory).</summary>
    public IReadOnlyCollection<CreateDeceasedMemoryRequest>? Memories { get; set; }
    /// <summary>Опциональные дополнительные метаданные.</summary>
    public CreateDeceasedMetadataRequest? Metadata { get; set; }
}

/// <summary>
/// Координаты и адресные атрибуты места захоронения в составе
/// <see cref="CreateDeceasedRequest"/>.
/// </summary>
public sealed class CreateDeceasedBurialLocationRequest
{
    /// <summary>Широта.</summary>
    public double Latitude { get; set; }
    /// <summary>Долгота.</summary>
    public double Longitude { get; set; }
    /// <summary>Заявленная точность GPS в метрах.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>Страна.</summary>
    public string? Country { get; set; }
    /// <summary>Регион/область.</summary>
    public string? Region { get; set; }
    /// <summary>Город или населённый пункт.</summary>
    public string? City { get; set; }
    /// <summary>Название кладбища.</summary>
    public string? CemeteryName { get; set; }
    /// <summary>Номер участка/сектора.</summary>
    public string? PlotNumber { get; set; }
    /// <summary>Номер могилы.</summary>
    public string? GraveNumber { get; set; }

    /// <summary>Уровень точности координат.</summary>
    public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.Exact;
}

/// <summary>
/// Одно воспоминание (memory) в составе <see cref="CreateDeceasedRequest"/>.
/// </summary>
public sealed class CreateDeceasedMemoryRequest
{
    /// <summary>Текст воспоминания.</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Дополнительные метаданные карточки умершего в составе
/// <see cref="CreateDeceasedRequest"/>.
/// </summary>
public sealed class CreateDeceasedMetadataRequest
{
    /// <summary>Эпитафия на надгробии.</summary>
    public string? Epitaph { get; set; }
    /// <summary>Вероисповедание.</summary>
    public string? Religion { get; set; }
    /// <summary>Источник сведений о захоронении.</summary>
    public string? Source { get; set; }
    /// <summary>Признак участия в военной службе.</summary>
    public bool IsMilitaryService { get; set; }
    /// <summary>Произвольная дополнительная информация.</summary>
    public string? AdditionalInfo { get; set; }
}
