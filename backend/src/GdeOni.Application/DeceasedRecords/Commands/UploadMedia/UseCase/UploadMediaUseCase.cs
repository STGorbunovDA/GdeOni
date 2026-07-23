using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;

public sealed class UploadMediaUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<UploadMediaUseCase> logger)
    : IUploadMediaUseCase
{
    public Task<Result<UploadMediaResponse, Error>> Execute(
        UploadMediaCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UploadMediaResponse, Error>> Handle(
        UploadMediaCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // После D11.3.4 FileKind удалён — UploadMediaCommand.Kind
        // напрямую MediaKind, MapKind не нужен.
        var validationResult = FileValidator.ValidateForKind(command.Kind, command.ContentType, command.SizeBytes);
        if (validationResult.IsFailure)
            return validationResult.Error;

        // D32: ValidateMagicBytesAsync теперь возвращает Stream, готовый
        // к стримингу в storage без Seek(0). Прочитанные первые 12 байт
        // склеиваются обратно через PrefixedStream — MinIO SDK получит
        // цельный поток за один проход.
        var magicBytesResult = await FileValidator.ValidateMagicBytesAsync(
            command.Content,
            command.ContentType,
            command.Kind,
            cancellationToken);
        if (magicBytesResult.IsFailure)
            return magicBytesResult.Error;

        var uploadStream = magicBytesResult.Value;

        // GetById без Include(Media) — Deceased.AddMedia больше не сканирует
        // _media по StorageKey (D7.35), уникальность держит unique-индекс +
        // Guid в storage key. Экономит RAM/трафик на карточках с большим
        // количеством media.
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var currentUserId = currentUserIdResult.Value;
        // D26. Выкладка медиа разрешена только админам — снимает юр.
        // риск от пользовательских загрузок чужих фото/документов.
        // Контроллер уже ограничен Roles=SuperAdmin,Admin; этот guard —
        // вторая линия защиты на случай прямого вызова use case'а.
        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMedia.UploadForbidden();

        var uploadRequest = new UploadFileRequest
        {
            Kind = command.Kind,
            DeceasedId = command.DeceasedId,
            OriginalFileName = command.OriginalFileName,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            // D32: uploadStream — это PrefixedStream вокруг command.Content
            // с уже встроенными первыми 12 байтами, поэтому MinIO получит
            // полный файл за один проход без Seek/повторного чтения.
            Content = uploadStream
        };

        var stored = await fileStorage.UploadAsync(uploadRequest, cancellationToken);

        var addResult = deceased.AddMedia(
            currentUserId,
            command.Kind,
            stored.OriginalFileName,
            stored.Bucket,
            stored.ObjectKey,
            stored.ContentType,
            stored.SizeBytes,
            command.Description);

        if (addResult.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return addResult.Error;
        }

        // D26. Загружать может только админ → его контент сразу
        // Approved. Модерация остаётся для legacy-media и для media,
        // которое было загружено до D26 в Pending-состоянии.
        var approveResult = addResult.Value.Approve();
        if (approveResult.IsFailure)
        {
            await TryDeleteFromStorage(stored.Bucket, stored.ObjectKey);
            return approveResult.Error;
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

        var media = addResult.Value;
        var (url, isPresigned) = await BuildUrl(media, cancellationToken);

        return Result.Success<UploadMediaResponse, Error>(new UploadMediaResponse(
            media.Id,
            media.DeceasedId,
            media.Bucket,
            media.StorageKey,
            media.ContentType,
            media.SizeBytes,
            url,
            isPresigned));
    }

    private async Task<(string Url, bool IsPresigned)> BuildUrl(
        DeceasedMedia media,
        CancellationToken cancellationToken)
    {
        if (media.Kind == MediaKind.Document)
        {
            var presigned = await fileStorage.GetPresignedUrlAsync(
                media.Bucket,
                media.StorageKey,
                MediaConstants.DocumentPresignedUrlTtl,
                cancellationToken);

            return (presigned, true);
        }

        return (fileStorage.GetPublicUrl(media.Bucket, media.StorageKey), false);
    }

    private async Task TryDeleteFromStorage(string bucket, string objectKey)
    {
        try
        {
            await fileStorage.DeleteAsync(bucket, objectKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось откатить загрузку файла из MinIO. Bucket: {Bucket}, Key: {Key}",
                bucket,
                objectKey);
        }
    }

}
