namespace GdeOni.Application.Events.Queries.GetHolidays.Model;

/// <summary>
/// Праздник в ответе API. <see cref="Category"/> — строковое имя
/// <see cref="HolidayCategory"/> (Memorial/Orthodox/Muslim/State/Fast),
/// клиент группирует по нему. <see cref="IsMajor"/> — «крупный» праздник:
/// клиент по нему ставит дефолтную галку напоминания «в день» и решает,
/// показывать ли попап.
/// </summary>
public sealed record HolidayDto(DateOnly Date, string Name, string Category, bool IsMajor);
