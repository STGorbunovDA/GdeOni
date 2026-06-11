using GdeOni.Application.Support.Queries.Common;

namespace GdeOni.Application.Support.Queries.GetById.Model;

public record GetSupportTicketByIdQuery(Guid TicketId);

public record GetSupportTicketByIdResponse(SupportTicketDto Ticket);
