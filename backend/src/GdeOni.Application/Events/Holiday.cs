namespace GdeOni.Application.Events;

/// <summary>
/// Одна памятная дата: когда, название, категория. Чистая модель без
/// привязки к транспорту — используется калькулятором и разворачивается
/// в DTO на уровне use case.
/// </summary>
public sealed record Holiday(DateOnly Date, string Name, HolidayCategory Category);
