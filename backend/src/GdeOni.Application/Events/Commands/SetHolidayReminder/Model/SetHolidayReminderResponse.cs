namespace GdeOni.Application.Events.Commands.SetHolidayReminder.Model;

/// <summary>Эхо сохранённой настройки: ключ праздника + нормализованный набор дней.</summary>
public sealed record SetHolidayReminderResponse(
    string HolidayKey,
    IReadOnlyList<int> LeadDays);
