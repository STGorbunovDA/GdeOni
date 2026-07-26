namespace GdeOni.Application.Events.Queries.GetMyHolidayReminders.Model;

/// <summary>
/// Явные настройки напоминаний пользователя. Только то, что он менял руками;
/// дефолты (крупные → «в день», мелкие → выключено) клиент считает сам по
/// флагу Holiday.IsMajor.
/// </summary>
public sealed record GetMyHolidayRemindersResponse(
    IReadOnlyList<HolidayReminderItem> Reminders);

/// <summary>
/// Одна настройка: ключ праздника (его имя) + набор «за сколько дней»
/// (0 = в день, 1, 3, 7). Пустой набор = напоминание отключено.
/// </summary>
public sealed record HolidayReminderItem(string HolidayKey, IReadOnlyList<int> LeadDays);
