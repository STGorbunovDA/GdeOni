using GdeOni.Domain.Aggregates.Support;

namespace GdeOni.Application.Support.Queries.Common;

internal static class SupportTicketMapping
{
    public static SupportTicketDto ToDto(this SupportTicket ticket, string? userEmail = null) =>
        new()
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            UserEmail = userEmail,
            Source = ticket.Source,
            Kind = ticket.Kind,
            Severity = ticket.Severity,
            Status = ticket.Status,
            Title = ticket.Title,
            Description = ticket.Description,
            Details = ticket.Details,
            ResolutionNote = ticket.ResolutionNote,
            ResolvedByUserId = ticket.ResolvedByUserId,
            ResolvedAtUtc = ticket.ResolvedAtUtc,
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
        };
}
