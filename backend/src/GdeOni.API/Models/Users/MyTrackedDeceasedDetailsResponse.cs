using GdeOni.API.Models.DeceasedRecords;

namespace GdeOni.API.Models.Users;

/// <summary>
/// Полный набор данных карточки умершего вместе с информацией о
/// персональной записи отслеживания текущего пользователя.
/// </summary>
public sealed class MyTrackedDeceasedDetailsResponse
{
    /// <summary>Данные карточки умершего.</summary>
    public DeceasedDetailsResponse Deceased { get; init; } = null!;
    /// <summary>Информация о записи отслеживания текущего пользователя.</summary>
    public MyTrackingInfoResponse Tracking { get; init; } = null!;
}

/// <summary>
/// Информация о персональной записи отслеживания умершего текущим
/// пользователем.
/// </summary>
public sealed class MyTrackingInfoResponse
{
    /// <summary>Идентификатор записи отслеживания.</summary>
    public Guid TrackingId { get; init; }
    /// <summary>Тип родственной связи (строкой из enum RelationshipType).</summary>
    public string RelationshipType { get; init; } = null!;
    /// <summary>Личные заметки пользователя об умершем.</summary>
    public string? PersonalNotes { get; init; }
    /// <summary>Получать уведомление к годовщине смерти (набор дней не пуст).</summary>
    public bool NotifyOnDeathAnniversary { get; init; }
    /// <summary>Получать уведомление к годовщине рождения (набор дней не пуст).</summary>
    public bool NotifyOnBirthAnniversary { get; init; }
    /// <summary>
    /// F42. Набор «за сколько дней» напоминать о годовщине смерти (0/1/3/7).
    /// Пустой список = напоминание выключено.
    /// </summary>
    public IReadOnlyList<int> DeathAnniversaryLeadDays { get; init; } = Array.Empty<int>();
    /// <summary>F42. Набор «за сколько дней» напоминать о годовщине рождения.</summary>
    public IReadOnlyList<int> BirthAnniversaryLeadDays { get; init; } = Array.Empty<int>();
    /// <summary>Статус отслеживания (строкой из enum TrackStatus).</summary>
    public string Status { get; init; } = null!;
    /// <summary>Дата и время добавления в отслеживание в UTC.</summary>
    public DateTime TrackedAtUtc { get; init; }
}
