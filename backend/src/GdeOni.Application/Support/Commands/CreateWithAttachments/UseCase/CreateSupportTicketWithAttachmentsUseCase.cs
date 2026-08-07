using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.CreateWithAttachments.Model;
using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.Support.Commands.CreateWithAttachments.UseCase;

/// <summary>
/// D33. Создание тикета с вложениями (1..5 файлов: JPEG/PNG до 10MB
/// или PDF до 25MB, суммарно ≤50MB). Поток:
/// 1) Валидация юзера и команды (FluentValidation).
/// 2) Для каждого файла: FileValidator (MIME + magic bytes + size).
///    Если хоть один файл невалидный — отбой ДО загрузки в MinIO.
/// 3) Создаём ticket (SupportTicket.CreateManual).
/// 4) Грузим каждый файл в MinIO (bucket support-attachments) и
///    добавляем Attachment через ticket.AddAttachment.
/// 5) Save в одну транзакцию.
/// 6) При ошибке Save — best-effort откат всех загруженных файлов.
/// </summary>
public sealed class CreateSupportTicketWithAttachmentsUseCase(
    ISupportTicketRepository ticketRepository,
    IFileStorage fileStorage,
    ISupportAttachmentsBucketResolver bucketResolver,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    INotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<CreateSupportTicketWithAttachmentsUseCase> logger)
    : ICreateSupportTicketWithAttachmentsUseCase
{
    public Task<Result<CreateSupportTicketWithAttachmentsResponse, Error>> Execute(
        CreateSupportTicketWithAttachmentsCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreateSupportTicketWithAttachmentsResponse, Error>> Handle(
        CreateSupportTicketWithAttachmentsCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D33. Сначала валидируем КАЖДЫЙ файл (MIME + magic bytes + size)
        // и фиксируем поток для последующей загрузки. Это делается ДО
        // создания тикета, чтобы при ошибке не оставлять в БД пустой
        // тикет без attachments.
        var validated = new List<(AttachmentUploadItem Item, Stream UploadStream)>(command.Attachments.Count);
        foreach (var item in command.Attachments)
        {
            // Тип определяется по contentType:
            //  image/* → DeceasedPhoto-кейс валидации (10MB лимит, magic для JPEG/PNG/WebP);
            //  application/pdf → Document-кейс (25MB, magic 25 50 44 46).
            // WebP юзер по UI прислать не может (мы его убрали из меню),
            // но если придёт — пройдёт магический байт. Это не риск:
            // bucket приватный, превью генерится клиентом.
            var fakeKind = item.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? MediaKind.DeceasedPhoto
                : MediaKind.Document;

            var typeSize = FileValidator.ValidateForKind(fakeKind, item.ContentType, item.SizeBytes);
            if (typeSize.IsFailure)
                return typeSize.Error;

            var magic = await FileValidator.ValidateMagicBytesAsync(
                item.Content, item.ContentType, fakeKind, cancellationToken);
            if (magic.IsFailure)
                return magic.Error;

            validated.Add((item, magic.Value));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var ticketResult = SupportTicket.CreateManual(
            currentUserIdResult.Value,
            command.Kind,
            command.Title,
            command.Description,
            nowUtc);

        if (ticketResult.IsFailure)
            return ticketResult.Error;

        var ticket = ticketResult.Value;
        var bucket = bucketResolver.GetBucket();
        var uploaded = new List<StoredFile>(validated.Count);

        try
        {
            foreach (var (item, uploadStream) in validated)
            {
                var stored = await fileStorage.UploadToBucketAsync(
                    bucket,
                    keyPrefix: $"tickets/{ticket.Id}",
                    item.OriginalFileName,
                    item.ContentType,
                    item.SizeBytes,
                    uploadStream,
                    cancellationToken);
                uploaded.Add(stored);

                var addResult = ticket.AddAttachment(
                    item.OriginalFileName,
                    stored.Bucket,
                    stored.ObjectKey,
                    item.ContentType,
                    item.SizeBytes,
                    nowUtc);
                if (addResult.IsFailure)
                    return addResult.Error;
            }

            await ticketRepository.Add(ticket, cancellationToken);
            await ticketRepository.Save(cancellationToken);
        }
        catch
        {
            await BestEffortDeleteAsync(uploaded);
            throw;
        }

        // Тикет сохранён — уведомляем SuperAdmin'ов (best-effort).
        await notificationService.NotifyRolesAsync(
            new[] { UserRole.SuperAdmin },
            NotificationKind.SupportTicketCreated,
            "Новое обращение",
            ticket.Title,
            $"/admin/support-tickets/{ticket.Id}",
            cancellationToken);

        return Result.Success<CreateSupportTicketWithAttachmentsResponse, Error>(
            new CreateSupportTicketWithAttachmentsResponse(ticket.Id, ticket.Attachments.Count));
    }

    private async Task BestEffortDeleteAsync(IEnumerable<StoredFile> uploaded)
    {
        foreach (var file in uploaded)
        {
            try
            {
                await fileStorage.DeleteAsync(file.Bucket, file.ObjectKey, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось откатить вложение тикета. Bucket: {Bucket}, Key: {Key}",
                    file.Bucket, file.ObjectKey);
            }
        }
    }
}
