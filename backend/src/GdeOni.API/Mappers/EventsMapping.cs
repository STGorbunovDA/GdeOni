using GdeOni.API.Models.Events;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Application.Events.Queries.GetHolidays.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Маппинг request-DTO событий → Application-query/command. Дефолты диапазона
/// проставляются здесь (presentation-слой), домен о них не знает.
/// </summary>
public static class EventsMapping
{
    /// <summary>По умолчанию: сегодня (UTC) … +30 дней.</summary>
    private const int DefaultRangeDays = 30;

    public static GetHolidaysQuery ToQuery(this GetHolidaysRequest request)
    {
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.To ?? from.AddDays(DefaultRangeDays);
        return new GetHolidaysQuery(from, to);
    }

    public static SetHolidayReminderCommand ToCommand(this SetHolidayReminderRequest request) =>
        new(request.HolidayKey ?? string.Empty, request.LeadDays ?? Array.Empty<int>());
}
