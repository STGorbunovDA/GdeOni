using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос обновления основных полей карточки умершего владельцем
/// карточки. Полная замена: все поля передаются всегда.
/// </summary>
public sealed class UpdateDeceasedRequest
{
    /// <summary>Имя умершего.</summary>
    public string FirstName { get; set; } = null!;
    /// <summary>Фамилия умершего.</summary>
    public string LastName { get; set; } = null!;
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

    /// <summary>Координаты и адресные атрибуты захоронения (необязательно).</summary>
    public UpdateDeceasedBurialLocationRequest? BurialLocation { get; set; }
    /// <summary>Дополнительные метаданные (эпитафия, конфессия и т.д.).</summary>
    public UpdateDeceasedMetadataRequest? Metadata { get; set; }
}

/// <summary>
/// Координаты и адресные атрибуты места захоронения в составе
/// <see cref="UpdateDeceasedRequest"/>.
/// </summary>
public sealed class UpdateDeceasedBurialLocationRequest
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
/// Дополнительные метаданные карточки умершего в составе
/// <see cref="UpdateDeceasedRequest"/>.
/// </summary>
public sealed class UpdateDeceasedMetadataRequest
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
