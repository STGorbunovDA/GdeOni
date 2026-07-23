namespace GdeOni.Application.Events.Queries.GetHolidays.Model;

/// <summary>
/// Ответ: праздники диапазона, отсортированные по дате.
/// </summary>
public sealed record GetHolidaysResponse(IReadOnlyList<HolidayDto> Holidays);
