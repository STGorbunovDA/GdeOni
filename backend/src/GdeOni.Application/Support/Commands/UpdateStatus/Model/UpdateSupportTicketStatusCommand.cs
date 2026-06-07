using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateStatus.Model;

/// <summary>
/// Админская смена <see cref="SupportTicketStatus"/> тикета. Если новый
/// статус — Resolved, обязателен <c>ResolutionNote</c>.
/// </summary>
public record UpdateSupportTicketStatusCommand(
    Guid TicketId,
    SupportTicketStatus Status,
    string? ResolutionNote);
