namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// Праздник из GET /api/events/holidays. Category — строковое имя
/// (Memorial/Orthodox/Muslim/State), клиент группирует по нему.
/// </summary>
public sealed record HolidayDto(DateOnly Date, string Name, string Category);

/// <summary>Ответ GET /api/events/holidays.</summary>
public sealed record GetHolidaysResponse(IReadOnlyList<HolidayDto> Holidays);
