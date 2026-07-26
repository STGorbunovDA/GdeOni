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
    /// <summary>
    /// DEPRECATED (F42): «получать уведомление к годовщине смерти». Старые
    /// клиенты шлют булев флаг; сервер маппит его в набор дней
    /// (true → «в день», false → выключено). Новые клиенты используют
    /// <see cref="DeathAnniversaryLeadDays"/> — если он передан, флаг игнорируется.
    /// </summary>
    public bool NotifyOnDeathAnniversary { get; set; }
    /// <summary>DEPRECATED (F42): см. <see cref="NotifyOnDeathAnniversary"/>.</summary>
    public bool NotifyOnBirthAnniversary { get; set; }
    /// <summary>
    /// F42. Набор «за сколько дней» напоминать о годовщине смерти: 0 = в день,
    /// 1, 3, 7. Пустой список = выключено. Если null — сервер берёт значение
    /// из устаревшего флага <see cref="NotifyOnDeathAnniversary"/>.
    /// </summary>
    public IReadOnlyList<int>? DeathAnniversaryLeadDays { get; set; }
    /// <summary>F42. Набор «за сколько дней» напоминать о годовщине рождения.</summary>
    public IReadOnlyList<int>? BirthAnniversaryLeadDays { get; set; }
    /// <summary>Статус отслеживания (активно/приостановлено и т.д.).</summary>
    public TrackStatus TrackStatus { get; set; }
}
