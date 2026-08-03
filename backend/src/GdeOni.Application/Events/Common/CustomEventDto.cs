namespace GdeOni.Application.Events.Common;

/// <summary>
/// Ручное событие пользователя. <see cref="Date"/> — якорь; напоминание
/// повторяется по месяцу/дню каждый год. <see cref="LeadDays"/> — «за сколько
/// дней» напоминать (0 = в день, 1, 3, 7); пустой набор = отключено.
/// </summary>
public sealed record CustomEventDto(
    Guid Id,
    string Title,
    DateOnly Date,
    IReadOnlyList<int> LeadDays);
