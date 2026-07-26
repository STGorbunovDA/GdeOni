namespace GdeOni.API.Models.Events;

/// <summary>
/// Тело PUT /api/events/holiday-reminders. <see cref="LeadDays"/> — набор
/// «за сколько дней напомнить» (0 = в день, 1, 3, 7). Пустой/отсутствует =
/// отключить напоминание о празднике.
/// </summary>
public sealed record SetHolidayReminderRequest(
    string? HolidayKey,
    IReadOnlyList<int>? LeadDays);
