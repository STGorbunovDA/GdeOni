namespace GdeOni.Application.Support.Queries.GetAttachmentById.Model;

public sealed record GetSupportAttachmentByIdQuery(Guid TicketId, Guid AttachmentId);

public sealed record GetSupportAttachmentByIdResponse(
    Guid AttachmentId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string PresignedUrl);
