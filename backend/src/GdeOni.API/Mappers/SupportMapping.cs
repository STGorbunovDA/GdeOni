using GdeOni.API.Models.Support;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Application.Support.Commands.UpdateSeverity.Model;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Application.Support.Queries.GetAll.Model;

namespace GdeOni.API.Mappers;

public static class SupportMapping
{
    public static CreateSupportTicketCommand ToCommand(this CreateSupportTicketRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateSupportTicketCommand(
            request.Kind,
            request.Title,
            request.Description);
    }

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

    public static UpdateSupportTicketStatusCommand ToCommand(
        this UpdateSupportTicketStatusRequest request, Guid ticketId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateSupportTicketStatusCommand(
            ticketId,
            request.Status,
            request.ResolutionNote);
    }

    public static UpdateSupportTicketSeverityCommand ToCommand(
        this UpdateSupportTicketSeverityRequest request, Guid ticketId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateSupportTicketSeverityCommand(ticketId, request.Severity);
    }
}
