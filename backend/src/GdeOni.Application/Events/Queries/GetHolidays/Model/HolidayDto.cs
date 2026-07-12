namespace GdeOni.Application.Events.Queries.GetHolidays.Model;

/// <summary>
/// Праздник в ответе API. <see cref="Category"/> — строковое имя
/// <see cref="HolidayCategory"/> (Memorial/Orthodox/Muslim/State),
/// клиент группирует по нему.
/// </summary>
public sealed record HolidayDto(DateOnly Date, string Name, string Category);
