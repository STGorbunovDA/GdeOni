using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

/// <summary>
/// Query-параметры админского листинга. Массивы статусов/severities
/// биндятся по стандартному ASP.NET-конвенцию (multiple ?statuses=Open
/// &amp;statuses=InProgress).
/// </summary>
public sealed class GetAllSupportTicketsRequest
{
    /// <summary>Фильтр по автору обращения.</summary>
    public Guid? UserId { get; set; }
    /// <summary>Фильтр по набору статусов.</summary>
    public SupportTicketStatus[]? Statuses { get; set; }
    /// <summary>Фильтр по набору уровней критичности.</summary>
    public SupportTicketSeverity[]? Severities { get; set; }
    /// <summary>Фильтр по типу обращения.</summary>
    public SupportTicketKind? Kind { get; set; }
    /// <summary>Фильтр по источнику обращения (web/mobile/system).</summary>
    public SupportTicketSource? Source { get; set; }
    /// <summary>Нижняя граница периода создания в UTC (включительно).</summary>
    public DateTime? CreatedFromUtc { get; set; }
    /// <summary>Верхняя граница периода создания в UTC (включительно).</summary>
    public DateTime? CreatedToUtc { get; set; }
    /// <summary>Подстрочный поиск по заголовку/тексту обращения.</summary>
    public string? Search { get; set; }
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
}
