using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос комплексной операции "пользователь у могилы": создание
/// карточки умершего вместе с координатами захоронения и автоматическим
/// добавлением умершего в персональный список отслеживания.
/// </summary>
public sealed class AddDeceasedAtGraveRequest
{
    /// <summary>Имя умершего.</summary>
    public string FirstName { get; set; } = null!;
    /// <summary>Фамилия умершего.</summary>
    public string LastName { get; set; } = null!;
    /// <summary>Отчество умершего (опционально).</summary>
    public string? MiddleName { get; set; }
    /// <summary>Дата рождения (опционально, если неизвестна).</summary>
    public DateOnly? BirthDate { get; set; }
    /// <summary>Дата смерти.</summary>
    public DateOnly DeathDate { get; set; }
    /// <summary>Короткое описание для превью карточки.</summary>
    public string? ShortDescription { get; set; }
    /// <summary>Развёрнутая биография.</summary>
    public string? Biography { get; set; }
    /// <summary>Координаты и адресные атрибуты захоронения.</summary>
    public AddDeceasedAtGraveLocationRequest GraveLocation { get; set; } = null!;
    /// <summary>Настройки отслеживания, с которыми карточка добавляется текущему пользователю.</summary>
    public AddDeceasedAtGraveTrackingRequest Tracking { get; set; } = null!;
}

/// <summary>
/// Координаты и адресные атрибуты места захоронения для сценария
/// "добавление умершего непосредственно у могилы".
/// </summary>
public sealed class AddDeceasedAtGraveLocationRequest
{
    /// <summary>Широта GPS-координаты.</summary>
    public double Latitude { get; set; }
    /// <summary>Долгота GPS-координаты.</summary>
    public double Longitude { get; set; }
    /// <summary>Заявленная погрешность GPS в метрах (если известна).</summary>
    public double? AccuracyMeters { get; set; }
    /// <summary>Страна (необязательно).</summary>
    public string? Country { get; set; }
    /// <summary>Город или населённый пункт (необязательно).</summary>
    public string? City { get; set; }
    /// <summary>Название кладбища (необязательно).</summary>
    public string? CemeteryName { get; set; }
    /// <summary>Номер участка/сектора (необязательно).</summary>
    public string? PlotNumber { get; set; }
    /// <summary>Номер могилы (необязательно).</summary>
    public string? GraveNumber { get; set; }
}

/// <summary>
/// Параметры отслеживания, с которыми только что созданная карточка
/// добавляется в персональный список текущего пользователя.
/// </summary>
public sealed class AddDeceasedAtGraveTrackingRequest
{
    /// <summary>Тип родственной связи пользователя с умершим.</summary>
    public RelationshipType RelationshipType { get; set; }
    /// <summary>Личные заметки пользователя об умершем.</summary>
    public string? PersonalNotes { get; set; }
    /// <summary>Получать уведомление к годовщине смерти.</summary>
    public bool NotifyOnDeathAnniversary { get; set; }
    /// <summary>Получать уведомление к годовщине рождения.</summary>
    public bool NotifyOnBirthAnniversary { get; set; }
}
