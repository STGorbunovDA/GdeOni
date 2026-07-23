using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос обновления записи отслеживания умершего текущим пользователем.
/// </summary>
public sealed class UpdateTrackingRequest
{
    /// <summary>Тип родственной связи пользователя с умершим.</summary>
    public RelationshipType RelationshipType { get; set; }
    /// <summary>Личные заметки пользователя об умершем.</summary>
    public string? PersonalNotes { get; set; }
    /// <summary>Получать уведомление к годовщине смерти.</summary>
    public bool NotifyOnDeathAnniversary { get; set; }
    /// <summary>Получать уведомление к годовщине рождения.</summary>
    public bool NotifyOnBirthAnniversary { get; set; }
    /// <summary>Статус отслеживания (активно/приостановлено и т.д.).</summary>
    public TrackStatus TrackStatus { get; set; }
}
