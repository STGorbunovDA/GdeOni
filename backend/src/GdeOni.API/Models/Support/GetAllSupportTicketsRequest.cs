using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

/// <summary>
/// Query-параметры админского листинга. Массивы статусов/severities
/// биндятся по стандартному ASP.NET-конвенцию (multiple ?statuses=Open
/// &amp;statuses=InProgress).
/// </summary>
public sealed class GetAllSupportTicketsRequest
{
    public Guid? UserId { get; set; }
    public SupportTicketStatus[]? Statuses { get; set; }
    public SupportTicketSeverity[]? Severities { get; set; }
    public SupportTicketKind? Kind { get; set; }
    public SupportTicketSource? Source { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
