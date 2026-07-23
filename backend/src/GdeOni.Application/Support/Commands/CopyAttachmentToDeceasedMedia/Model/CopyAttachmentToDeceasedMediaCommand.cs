using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Model;

/// <summary>
/// D35. Скопировать вложение из тикета поддержки в media умершего.
/// Универсальный вариант: MediaKind (DeceasedPhoto / GravePhoto /
/// Document) + флаг MakeMain (только при DeceasedPhoto).
/// Server-side MinIO copy: support-attachments → bucket для kind.
/// Вложение в тикете остаётся.
/// </summary>
public sealed record CopyAttachmentToDeceasedMediaCommand(
    Guid TicketId,
    Guid AttachmentId,
    Guid DeceasedId,
    MediaKind MediaKind,
    bool MakeMain);

public sealed record CopyAttachmentToDeceasedMediaResponse(Guid MediaId);
