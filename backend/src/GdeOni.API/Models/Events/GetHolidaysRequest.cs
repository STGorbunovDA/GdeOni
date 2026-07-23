namespace GdeOni.API.Models.Events;

/// <summary>
/// Query-параметры запроса праздников. Обе даты опциональны: без них
/// берётся диапазон «сегодня (UTC) … +30 дней». Клиент обычно передаёт
/// свои локальные даты явно.
/// </summary>
public sealed class GetHolidaysRequest
{
    /// <summary>Начало диапазона (ISO yyyy-MM-dd). По умолчанию — сегодня (UTC).</summary>
    public DateOnly? From { get; init; }

    /// <summary>Конец диапазона (ISO yyyy-MM-dd). По умолчанию — From + 30 дней.</summary>
    public DateOnly? To { get; init; }
}
