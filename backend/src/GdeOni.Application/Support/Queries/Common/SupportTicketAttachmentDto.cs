namespace GdeOni.Application.Support.Queries.Common;

/// <summary>
/// D33. DTO вложения в тикете. URL для скачивания клиент получает
/// отдельным запросом GET /api/support-tickets/{ticketId}/attachments/{id}
/// — здесь только метаданные, без presigned URL'ов (TTL короткий,
/// нет смысла раздавать при каждом GetById).
/// </summary>
public sealed class SupportTicketAttachmentDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
