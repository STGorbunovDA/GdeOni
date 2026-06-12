namespace GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Model;

/// <summary>
/// D35. Сделать вложение тикета (фото) главным фото указанного
/// умершего. Админ-операция; вложение остаётся в тикете (история
/// переписки), копия попадает в deceased-photos.
/// </summary>
public sealed record PromoteAttachmentToMainPhotoCommand(
    Guid TicketId,
    Guid AttachmentId,
    Guid DeceasedId);

public sealed record PromoteAttachmentToMainPhotoResponse(Guid MediaId);
