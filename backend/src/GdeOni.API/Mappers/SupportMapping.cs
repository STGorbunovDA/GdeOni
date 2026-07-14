using GdeOni.API.Models.Support;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Application.Support.Commands.ForceClose.Model;
using GdeOni.Application.Support.Commands.UpdateSeverity.Model;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Application.Support.Queries.GetAll.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Command/Query маппинг для контроллеров поддержки
/// (обращения пользователей).
/// </summary>
public static class SupportMapping
{
    /// <summary>Маппит DTO создания обращения в команду use case.</summary>
    public static CreateSupportTicketCommand ToCommand(this CreateSupportTicketRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateSupportTicketCommand(
            request.Kind,
            request.Title,
            request.Description);
    }

    /// <summary>Маппит DTO админского листинга обращений в запрос use case.</summary>
    public static GetAllSupportTicketsQuery ToQuery(this GetAllSupportTicketsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetAllSupportTicketsQuery(
            request.UserId,
            request.Statuses,
            request.Severities,
            request.Kind,
            request.Source,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            request.Search,
            request.Page,
            request.PageSize);
    }

    /// <summary>Маппит DTO смены статуса обращения в команду use case.</summary>
    public static UpdateSupportTicketStatusCommand ToCommand(
        this UpdateSupportTicketStatusRequest request, Guid ticketId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateSupportTicketStatusCommand(
            ticketId,
            request.Status,
            request.ResolutionNote);
    }

    /// <summary>Маппит DTO смены критичности обращения в команду use case.</summary>
    public static UpdateSupportTicketSeverityCommand ToCommand(
        this UpdateSupportTicketSeverityRequest request, Guid ticketId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateSupportTicketSeverityCommand(ticketId, request.Severity);
    }

    /// <summary>D40. Маппит DTO принудительного закрытия в команду use case.</summary>
    public static ForceCloseSupportTicketCommand ToCommand(
        this ForceCloseSupportTicketRequest request, Guid ticketId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ForceCloseSupportTicketCommand(ticketId, request.CloseNote);
    }
}
