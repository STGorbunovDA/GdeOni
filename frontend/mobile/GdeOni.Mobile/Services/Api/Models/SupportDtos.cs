namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// D25 mobile. DTO обращения (бэкенд называет это support_ticket; в UI
/// у нас слово "обращение", чтобы юзер не вспоминал tech-сленг).
/// Enum'ы передаются строками — соответствует JsonStringEnumConverter
/// на бэке.
/// </summary>
public sealed record SupportTicketDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Source,    // "Manual" / "Auto"
    string Kind,      // "Payment" / "Bug" / "Complaint" / "Question" / "Other"
    string Severity,  // "Normal" / "Urgent"
    string Status,    // "Open" / "InProgress" / "Resolved"
    string Title,
    string Description,
    string? Details,
    string? ResolutionNote,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    bool AcceptedByUser,
    DateTime? AcceptedByUserAtUtc,
    string? LastUserReply,
    DateTime? LastUserReplyAtUtc,
    int ReopenedCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<SupportTicketMessageDto>? Messages = null);

public sealed record SupportTicketMessageDto(
    Guid Id,
    string AuthorKind,   // "User" / "Admin"
    Guid? AuthorUserId,
    string Text,
    DateTime CreatedAtUtc);

public sealed record CreateSupportTicketRequest(
    string Kind,
    string Title,
    string Description);

public sealed record CreateSupportTicketResponse(Guid TicketId);

public sealed record GetMySupportTicketsResponse(
    IReadOnlyList<SupportTicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record GetAllSupportTicketsResponse(
    IReadOnlyList<SupportTicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record GetSupportTicketByIdResponse(SupportTicketDto Ticket);

public sealed record UpdateSupportTicketStatusRequest(
    string Status,
    string? ResolutionNote);

public sealed record UpdateSupportTicketSeverityRequest(string Severity);

public sealed record ReopenSupportTicketRequest(string? UserReply);
