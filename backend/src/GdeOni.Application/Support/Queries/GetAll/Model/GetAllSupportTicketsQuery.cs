using GdeOni.Application.Support.Queries.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAll.Model;

/// <summary>
/// Админский листинг тикетов. Статусы и severity — массивы для
/// чек-боксного фильтра в UI (несколько одновременно). search ищет
/// в title / description / email юзера.
/// </summary>
public record GetAllSupportTicketsQuery(
    Guid? UserId,
    SupportTicketStatus[]? Statuses,
    SupportTicketSeverity[]? Severities,
    SupportTicketKind? Kind,
    SupportTicketSource? Source,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    string? Search,
    int Page,
    int PageSize);

public record GetAllSupportTicketsResponse(
    List<SupportTicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
