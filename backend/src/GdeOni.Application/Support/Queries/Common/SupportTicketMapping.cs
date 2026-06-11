using GdeOni.Domain.Aggregates.Support;

namespace GdeOni.Application.Support.Queries.Common;

internal static class SupportTicketMapping
{
    public static SupportTicketDto ToDto(
        this SupportTicket ticket,
        string? userEmail = null,
        bool includeMessages = false) =>
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
            AcceptedByUser = ticket.AcceptedByUser,
            AcceptedByUserAtUtc = ticket.AcceptedByUserAtUtc,
            LastUserReply = ticket.LastUserReply,
            LastUserReplyAtUtc = ticket.LastUserReplyAtUtc,
            ReopenedCount = ticket.ReopenedCount,
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
            Messages = includeMessages
                ? ticket.Messages
                    .OrderBy(m => m.CreatedAtUtc)
                    .Select(m => new SupportTicketMessageDto
                    {
                        Id = m.Id,
                        AuthorKind = m.AuthorKind,
                        AuthorUserId = m.AuthorUserId,
                        Text = m.Text,
                        CreatedAtUtc = m.CreatedAtUtc,
                    })
                    .ToList()
                : null,
        };
}
