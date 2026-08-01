using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;

public sealed class GetMediaByIdUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMediaByIdUseCase
{
    public Task<Result<MediaDetailsResponse, Error>> Execute(
        GetMediaByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<MediaDetailsResponse, Error>> Handle(
        GetMediaByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D7.47: filtered Include — грузим только одну media.
        var deceased = await deceasedRepository.GetByIdWithMediaById(
            query.DeceasedId, query.MediaId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        var media = deceased.Media.FirstOrDefault(x => x.Id == query.MediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(query.MediaId);

        // D11.13.1: Pending/Rejected media видят только uploader,
        // автор карточки и админ. Остальным — NotFound (как и в
        // GetMediaListUseCase, который для outsider'а принудительно
        // выставляет moderationStatus=Approved). Иначе авторизованный
        // юзер с Guid media скачивает файл в обход модерации.
        if (media.ModerationStatus != ModerationStatus.Approved)
        {
            var currentUserId = currentUserIdResult.Value;
            var canSeePending = currentUserService.IsAdmin()
                || media.UploadedByUserId == currentUserId
                || deceased.CreatedByUserId == currentUserId;
            if (!canSeePending)
                return Errors.DeceasedMedia.NotFound(query.MediaId);
        }

        var (url, isPresigned) = await BuildUrl(media, cancellationToken);

        return Result.Success<MediaDetailsResponse, Error>(new MediaDetailsResponse
        {
            Id = media.Id,
            DeceasedId = media.DeceasedId,
            UploadedByUserId = media.UploadedByUserId,
            Kind = media.Kind.ToString(),
            OriginalFileName = media.OriginalFileName,
            Bucket = media.Bucket,
            StorageKey = media.StorageKey,
            ContentType = media.ContentType,
            SizeBytes = media.SizeBytes,
            Description = media.Description,
            IsMainPhoto = media.IsMainPhoto,
            ModerationStatus = media.ModerationStatus.ToString(),
            Url = url,
            IsPresigned = isPresigned,
            CreatedAtUtc = media.CreatedAtUtc,
            UpdatedAtUtc = media.UpdatedAtUtc
        });
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

        // D47: фото — путь к «вахтёру» (авторизованный стрим через сервер),
        // не прямой публичный URL MinIO. isPresigned=false: клиент грузит
        // его своим авторизованным клиентом, как и остальные фото.
        return (fileStorage.GetPhotoContentPath(media.Id), false);
    }
}
