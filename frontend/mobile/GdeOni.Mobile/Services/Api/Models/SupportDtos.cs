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
    string Kind,      // "Payment" / "Bug" / "Complaint" / "Question" / "Other" / "Photo"
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
    IReadOnlyList<SupportTicketMessageDto>? Messages = null,
    IReadOnlyList<SupportTicketAttachmentDto>? Attachments = null);

/// <summary>
/// D33. Вложение в обращении (фото или PDF). URL для скачивания
/// клиент получает отдельным запросом GetAttachmentAsync — здесь
/// только метаданные.
/// </summary>
public sealed record SupportTicketAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);

public sealed record GetSupportAttachmentByIdResponse(
    Guid AttachmentId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string PresignedUrl);

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

public sealed record CreateSupportTicketWithAttachmentsResponse(Guid TicketId, int AttachmentsCount);

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
