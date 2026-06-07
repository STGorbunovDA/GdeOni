using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateSeverity.Model;

public record UpdateSupportTicketSeverityCommand(
    Guid TicketId,
    SupportTicketSeverity Severity);
