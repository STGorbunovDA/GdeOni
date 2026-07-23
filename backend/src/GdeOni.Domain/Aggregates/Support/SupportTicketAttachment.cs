using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Support;

/// <summary>
/// D33. Вложение в обращение в поддержку. Юзер прикладывает к тикету
/// до 5 файлов (фото / документы) — например, фото умершего и доки
/// о наследстве. Файл хранится в MinIO (bucket support-attachments),
/// метаданные — в support_ticket_attachments. Принадлежит агрегату
/// <see cref="SupportTicket"/>: добавление только через
/// <see cref="SupportTicket.AddAttachment"/>, наружу выдаётся как
/// IReadOnlyCollection.
///
/// <para>
/// Public URL не выдаётся: вложения могут содержать персональные
/// данные (паспорт, свидетельство о рождении), доступ только через
/// presigned URL с TTL.
/// </para>
/// </summary>
public sealed class SupportTicketAttachment : Entity<Guid>
{
    public const int MaxFileNameLength = 256;

    public Guid TicketId { get; private set; }
    public string OriginalFileName { get; private set; }
    public string Bucket { get; private set; }
    public string StorageKey { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public DateTime UploadedAtUtc { get; }

    private SupportTicketAttachment() : base(Guid.Empty)
    {
        OriginalFileName = null!;
        Bucket = null!;
        StorageKey = null!;
        ContentType = null!;
    }

    private SupportTicketAttachment(
        Guid id,
        Guid ticketId,
        string originalFileName,
        string bucket,
        string storageKey,
        string contentType,
        long sizeBytes,
        DateTime uploadedAtUtc) : base(id)
    {
        TicketId = ticketId;
        OriginalFileName = originalFileName;
        Bucket = bucket;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAtUtc = uploadedAtUtc;
    }

    internal static Result<SupportTicketAttachment, Error> Create(
        Guid ticketId,
        string originalFileName,
        string bucket,
        string storageKey,
        string contentType,
        long sizeBytes,
        DateTime nowUtc)
    {
        if (ticketId == Guid.Empty)
            return Errors.General.ValueIsRequired("ticketId");

        if (string.IsNullOrWhiteSpace(originalFileName))
            return Errors.Support.AttachmentFileNameRequired();

        var trimmedName = originalFileName.Trim();
        if (trimmedName.Length > MaxFileNameLength)
            return Errors.Support.AttachmentFileNameTooLong(MaxFileNameLength);

        if (string.IsNullOrWhiteSpace(bucket))
            return Errors.General.ValueIsRequired("bucket");

        if (string.IsNullOrWhiteSpace(storageKey))
            return Errors.General.ValueIsRequired("storageKey");

        if (string.IsNullOrWhiteSpace(contentType))
            return Errors.Support.AttachmentContentTypeRequired();

        if (sizeBytes <= 0)
            return Errors.Support.AttachmentSizeInvalid();

        return new SupportTicketAttachment(
            Guid.NewGuid(),
            ticketId,
            trimmedName,
            bucket,
            storageKey,
            contentType,
            sizeBytes,
            nowUtc);
    }
}
