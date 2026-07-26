namespace GdeOni.Application.Events.Commands.SetHolidayReminder.Model;

/// <summary>
/// Задать/обновить напоминание о празднике для текущего пользователя.
/// <see cref="HolidayKey"/> — имя праздника (стабильный ключ). <see cref="LeadDays"/>
/// — набор «за сколько дней напомнить» (0 = в день, 1, 3, 7). Пустой набор =
/// отключить напоминание.
/// </summary>
public sealed record SetHolidayReminderCommand(
    string HolidayKey,
    IReadOnlyList<int> LeadDays);
