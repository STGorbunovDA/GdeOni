using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Create.Model;

public record CreateSupportTicketCommand(
    SupportTicketKind Kind,
    string Title,
    string Description);

public record CreateSupportTicketResponse(Guid TicketId);
