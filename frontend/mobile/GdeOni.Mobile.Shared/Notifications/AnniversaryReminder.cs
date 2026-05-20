namespace GdeOni.Mobile.Shared.Notifications;

/// <summary>
/// E23. Описание напоминания для планировщика. Платформенный код
/// получает этот record и сам решает как срабатывать (Android
/// AlarmManager + NotificationChannel; iOS UNUserNotificationCenter
/// в будущем).
/// </summary>
/// <param name="DeceasedId">
/// Стабильный идентификатор для генерации тега запроса
/// ("anniv-{id}-{kind}") — позволяет cancel'у найти и переписать
/// существующий alarm.
/// </param>
/// <param name="FullName">Отображается в теле notification ("Сегодня
/// годовщина рождения {name}").</param>
/// <param name="EventDate">
/// Оригинальная дата события (рождения или смерти). Планировщик сам
/// считает ближайшую годовщину после nowUtc через
/// <see cref="AnniversaryDateCalculator.NextAnniversary"/>.
/// </param>
/// <param name="Kind">Birth или Death — определяет текст уведомления.</param>
public sealed record AnniversaryReminder(
    Guid DeceasedId,
    string FullName,
    DateOnly EventDate,
    AnniversaryKind Kind);
