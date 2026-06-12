using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CreateWithAttachments.Model;

/// <summary>
/// D33. Создание тикета поддержки с вложениями. Аналог
/// CreateSupportTicketCommand, но с массивом файлов (до 5).
/// Старая ручка без вложений остаётся для случаев когда юзер
/// не прикладывает ничего — multipart-overhead не нужен.
/// </summary>
public sealed class CreateSupportTicketWithAttachmentsCommand
{
    public required SupportTicketKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<AttachmentUploadItem> Attachments { get; init; }
}

public sealed class AttachmentUploadItem
{
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required Stream Content { get; init; }
}

public sealed record CreateSupportTicketWithAttachmentsResponse(Guid TicketId, int AttachmentsCount);
