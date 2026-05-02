using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;

public sealed class GetMediaListUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMediaListUseCase
{
    public Task<Result<PagedResponse<MediaListItemResponse>, Error>> Execute(
        GetMediaListQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedResponse<MediaListItemResponse>, Error>> Handle(
        GetMediaListQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var deceased = await deceasedRepository.GetById(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        // Pending/Rejected media — модерационный контекст. Их видит только
        // автор карточки и админы. Любой другой авторизованный юзер
        // получает только Approved независимо от того, что попросил
        // в ?moderationStatus=…
        var isAdmin = currentUserService.IsAdmin();
        var isOwner = deceased.CreatedByUserId == currentUserIdResult.Value;
        var moderationStatus = (isAdmin || isOwner)
            ? query.ModerationStatus
            : ModerationStatus.Approved;

        var (items, totalCount) = await deceasedRepository.GetMediaPaged(
            query.DeceasedId,
            query.Kind,
            moderationStatus,
            query.Page,
            query.PageSize,
            cancellationToken);

        var responseItems = await Task.WhenAll(items.Select(async media =>
        {
            var (url, isPresigned) = await BuildUrl(media, cancellationToken);
            return MapToResponse(media, url, isPresigned);
        }));

        var response = new PagedResponse<MediaListItemResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<MediaListItemResponse>, Error>(response);
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

    private static MediaListItemResponse MapToResponse(DeceasedMedia media, string url, bool isPresigned) =>
        new()
        {
            Id = media.Id,
            DeceasedId = media.DeceasedId,
            UploadedByUserId = media.UploadedByUserId,
            Kind = media.Kind.ToString(),
            OriginalFileName = media.OriginalFileName,
            ContentType = media.ContentType,
            SizeBytes = media.SizeBytes,
            Description = media.Description,
            IsMainPhoto = media.IsMainPhoto,
            ModerationStatus = media.ModerationStatus.ToString(),
            Url = url,
            IsPresigned = isPresigned,
            CreatedAtUtc = media.CreatedAtUtc,
            UpdatedAtUtc = media.UpdatedAtUtc
        };
}
