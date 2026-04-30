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

        var domainKind = MapKind(command.Kind);
        var validationResult = FileValidator.ValidateForKind(domainKind, command.ContentType, command.SizeBytes);
        if (validationResult.IsFailure)
            return validationResult.Error;

        var deceased = await deceasedRepository.GetByIdWithMedia(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();
        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.DeceasedMedia.UploadForbidden();

        var uploadRequest = new UploadFileRequest
        {
            Kind = command.Kind,
            DeceasedId = command.DeceasedId,
            OriginalFileName = command.OriginalFileName,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            Content = command.Content
        };

        var stored = await fileStorage.UploadAsync(uploadRequest, cancellationToken);

        var addResult = deceased.AddMedia(
            currentUserId,
            domainKind,
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

    private static MediaKind MapKind(FileKind kind) => kind switch
    {
        FileKind.DeceasedPhoto => MediaKind.DeceasedPhoto,
        FileKind.GravePhoto => MediaKind.GravePhoto,
        FileKind.Document => MediaKind.Document,
        FileKind.Other => MediaKind.Other,
        _ => MediaKind.Other
    };
}
