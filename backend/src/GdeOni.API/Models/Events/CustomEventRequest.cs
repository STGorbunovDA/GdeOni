namespace GdeOni.API.Models.Events;

/// <summary>Тело POST/PUT ручного события. LeadDays — «за сколько дней» (0/1/3/7).</summary>
public sealed class CustomEventRequest
{
    /// <summary>Название события (например, «ДР друга»).</summary>
    public string Title { get; set; } = null!;

    /// <summary>Дата события (ISO yyyy-MM-dd). Повторяется каждый год по дню/месяцу.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Набор «за сколько дней» напоминать. Пустой = напоминание отключено.</summary>
    public IReadOnlyList<int> LeadDays { get; set; } = new List<int>();
}
