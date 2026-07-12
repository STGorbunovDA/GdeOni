namespace GdeOni.Application.Events.Queries.GetHolidays.Model;

/// <summary>
/// Запрос праздников в диапазоне дат [From, To] включительно.
/// </summary>
public sealed record GetHolidaysQuery(DateOnly From, DateOnly To);
