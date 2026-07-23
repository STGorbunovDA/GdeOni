using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.UseCase;

/// <summary>
/// D35. Универсальная операция "скопировать вложение тикета в media
/// умершего":
///   — MediaKind.DeceasedPhoto (+ MakeMain=true) — главное фото;
///   — MediaKind.DeceasedPhoto (MakeMain=false) — просто в галерею;
///   — MediaKind.GravePhoto — фото могилы;
///   — MediaKind.Document — документ умершего (для PDF).
///
/// Только админ. Server-side MinIO copy → AddMedia (Approve) →
/// опционально SetMainPhoto. Вложение в тикете НЕ удаляется.
/// Согласование contentType ↔ MediaKind:
///   image/* → DeceasedPhoto или GravePhoto;
///   application/pdf → Document.
/// Чужие комбинации отбиваем ContentTypeMismatch.
/// </summary>
public sealed class CopyAttachmentToDeceasedMediaUseCase(
    ISupportTicketRepository ticketRepository,
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<CopyAttachmentToDeceasedMediaUseCase> logger)
    : ICopyAttachmentToDeceasedMediaUseCase
{
    public Task<Result<CopyAttachmentToDeceasedMediaResponse, Error>> Execute(
        CopyAttachmentToDeceasedMediaCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CopyAttachmentToDeceasedMediaResponse, Error>> Handle(
        CopyAttachmentToDeceasedMediaCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsSuperAdmin())
            return Errors.DeceasedMedia.UploadForbidden();

        var ticket = await ticketRepository.GetByIdWithAttachments(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.Support.AttachmentNotFound();

        var attachment = ticket.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId);
        if (attachment is null)
            return Errors.Support.AttachmentNotFound();

        // Согласование contentType с целевым MediaKind. Фото идёт
        // как фото, PDF — как документ. Без этого админ мог бы
        // случайно положить PDF в "галерею фото".
        var typeOk = command.MediaKind switch
        {
            MediaKind.DeceasedPhoto or MediaKind.GravePhoto =>
                attachment.ContentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase),
            MediaKind.Document =>
                string.Equals(attachment.ContentType, "application/pdf", System.StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
        if (!typeOk)
            return Errors.DeceasedMedia.KindInvalid();

        var deceased = await deceasedRepository.GetByIdWithMedia(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        // Server-side copy в нужный bucket по MediaKind.
        var stored = await fileStorage.CopyObjectByKindAsync(
            sourceBucket: attachment.Bucket,
            sourceObjectKey: attachment.StorageKey,
            destKind: command.MediaKind,
            deceasedId: deceased.Id,
            fileName: attachment.OriginalFileName,
            contentType: attachment.ContentType,
            sizeBytes: attachment.SizeBytes,
            cancellationToken: cancellationToken);

        var addResult = deceased.AddMedia(
            currentUserIdResult.Value,
            command.MediaKind,
            stored.OriginalFileName,
            stored.Bucket,
            stored.ObjectKey,
            stored.ContentType,
            stored.SizeBytes,
            description: $"Из обращения {ticket.Id}");

        if (addResult.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return addResult.Error;
        }

        // Админ — auto-approve (как в UploadMedia D26).
        var approve = addResult.Value.Approve();
        if (approve.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return approve.Error;
        }

        if (command.MakeMain)
        {
            var setMain = deceased.SetMainPhoto(addResult.Value.Id);
            if (setMain.IsFailure)
            {
                await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
                return setMain.Error;
            }
        }

        try
        {
            await deceasedRepository.Save(cancellationToken);
        }
        catch
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            throw;
        }

        return Result.Success<CopyAttachmentToDeceasedMediaResponse, Error>(
            new CopyAttachmentToDeceasedMediaResponse(addResult.Value.Id));
    }

    private async Task TryDeleteFromStorage(string bucket, string objectKey)
    {
        try
        {
            await fileStorage.DeleteAsync(bucket, objectKey, CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось откатить копию вложения тикета в media. Bucket: {Bucket}, Key: {Key}",
                bucket, objectKey);
        }
    }
}
