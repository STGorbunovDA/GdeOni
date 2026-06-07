using GdeOni.Application.Support.Queries.Common;

namespace GdeOni.Application.Support.Queries.GetMine.Model;

/// <summary>
/// "Мои обращения" — листинг тикетов текущего юзера. UserId не в DTO —
/// берётся из JWT.
/// </summary>
public record GetMySupportTicketsQuery(int Page, int PageSize);

public record GetMySupportTicketsResponse(
    List<SupportTicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
