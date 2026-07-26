namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// Праздник из GET /api/events/holidays. Category — строковое имя
/// (Memorial/Orthodox/Muslim/State/Fast), клиент группирует по нему.
/// IsMajor — «крупный» праздник: по нему показываем попап «сегодня праздник».
/// </summary>
public sealed record HolidayDto(DateOnly Date, string Name, string Category, bool IsMajor);

/// <summary>Ответ GET /api/events/holidays.</summary>
public sealed record GetHolidaysResponse(IReadOnlyList<HolidayDto> Holidays);
