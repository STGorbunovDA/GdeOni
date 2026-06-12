using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.UseCase;

/// <summary>
/// D35. Берёт фото из вложения тикета поддержки и устанавливает его
/// как главное фото указанного умершего. Только админ.
///
/// Pipeline:
/// 1) Грузим тикет с Attachments — находим нужное вложение.
/// 2) Проверяем что это фото (image/*) — PDF нельзя как фото.
/// 3) Грузим Deceased с media.
/// 4) MinIO server-side copy: support-attachments → deceased-photos.
/// 5) deceased.AddMedia (Pending) → Approve → SetMainPhoto.
/// 6) Save. Если что-то упало после copy — удаляем созданную копию.
///
/// Вложение в тикете НЕ удаляется (запрос юзера: "оставляем
/// в качестве дублирования").
/// </summary>
public sealed class PromoteAttachmentToMainPhotoUseCase(
    ISupportTicketRepository ticketRepository,
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<PromoteAttachmentToMainPhotoUseCase> logger)
    : IPromoteAttachmentToMainPhotoUseCase
{
    public Task<Result<PromoteAttachmentToMainPhotoResponse, Error>> Execute(
        PromoteAttachmentToMainPhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<PromoteAttachmentToMainPhotoResponse, Error>> Handle(
        PromoteAttachmentToMainPhotoCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // Только админ. UI-кнопка скрыта, но это вторая линия защиты
        // от прямого вызова use case'а — как в UploadMediaUseCase (D26).
        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMedia.UploadForbidden();

        var ticket = await ticketRepository.GetByIdWithAttachments(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.Support.AttachmentNotFound();

        var attachment = ticket.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId);
        if (attachment is null)
            return Errors.Support.AttachmentNotFound();

        // PDF нельзя сделать фото умершего.
        if (!attachment.ContentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
            return Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain();

        var deceased = await deceasedRepository.GetByIdWithMedia(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        // Целевой bucket для фото умершего получаем через MediaKind-based
        // pipeline: используем UploadFileRequest только чтобы узнать
        // bucket из существующего media (если есть). Иначе CopyObjectAsync
        // не знает имени bucket'а — он не должен знать. Передаём через
        // отдельный resolver-метод от инфраструктуры.
        var destBucket = await GetDeceasedPhotosBucketAsync(deceased.Id, cancellationToken);

        // Server-side copy в MinIO — не качаем себе, не загружаем заново.
        var stored = await fileStorage.CopyObjectAsync(
            sourceBucket: attachment.Bucket,
            sourceObjectKey: attachment.StorageKey,
            destBucket: destBucket,
            destKeyPrefix: $"deceasedphoto/{deceased.Id}",
            fileName: attachment.OriginalFileName,
            contentType: attachment.ContentType,
            sizeBytes: attachment.SizeBytes,
            cancellationToken: cancellationToken);

        var addResult = deceased.AddMedia(
            currentUserIdResult.Value,
            MediaKind.DeceasedPhoto,
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

        // Админ — auto-approve, как в UploadMediaUseCase D26.
        var approve = addResult.Value.Approve();
        if (approve.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return approve.Error;
        }

        var setMain = deceased.SetMainPhoto(addResult.Value.Id);
        if (setMain.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return setMain.Error;
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

        return Result.Success<PromoteAttachmentToMainPhotoResponse, Error>(
            new PromoteAttachmentToMainPhotoResponse(addResult.Value.Id));
    }

    /// <summary>
    /// Имя bucket'а для фото умершего. Хардкодим через резолвер —
    /// в Application мы не знаем про MinioOptions, но и DeceasedPhotos
    /// bucket уже фигурирует в IFileStorage.UploadAsync(kind), так что
    /// логично достать его тем же путём через тестовую upload (не делаем).
    /// Берём через отдельный IDeceasedPhotosBucketResolver.
    /// </summary>
    private async Task<string> GetDeceasedPhotosBucketAsync(System.Guid deceasedId, CancellationToken ct)
    {
        // Если у умершего уже есть DeceasedPhoto — берём bucket оттуда.
        // Это всегда корректное имя, так как UploadAsync(DeceasedPhoto)
        // именно его и проставляет в Media.Bucket. Иначе — fallback на
        // дефолтный "deceased-photos" (соответствует MinioBucketsOptions
        // дефолту, читать MinioOptions из Application нельзя).
        var deceased = await deceasedRepository.GetByIdWithMedia(deceasedId, ct);
        var bucket = deceased?.Media
            .FirstOrDefault(m => m.Kind == MediaKind.DeceasedPhoto)?.Bucket;
        return string.IsNullOrEmpty(bucket) ? "deceased-photos" : bucket;
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
                "Не удалось откатить копию фото из тикета в media. Bucket: {Bucket}, Key: {Key}",
                bucket, objectKey);
        }
    }
}
