using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;

public sealed class GetAllDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAllDeceasedUseCase
{
    public Task<Result<PagedResponse<GetAllDeceasedItemResponse>, Error>> Execute(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedResponse<GetAllDeceasedItemResponse>, Error>> Handle(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D15: GetAll открыт всем авторизованным пользователям. Это нужно
        // для функции "поиск существующего умершего перед созданием новой
        // карточки" (E16 на mobile): юзер у могилы сначала ищет, может
        // её уже добавил кто-то другой → подписаться, не плодя дубликаты.
        // Verify/Unverify админский endpoint оставлен (IsVerified — это
        // информер "проверено редакцией", не gate). Errors.Deceased.
        // InsufficientPermissionsToViewAllDeceased оставлен в Errors на
        // случай если ещё где-то понадобится — но в GetAll больше не зовётся.

        var (items, totalCount) = await deceasedRepository.GetPaged(query, cancellationToken);

        // Лекарство от N+1: одним SQL'ем тянем главные фото для всех
        // карточек страницы. GetPaged не Include'ит Media, поэтому
        // GetMainPhoto() на этих сущностях вернул бы null. Approved-
        // фильтр зашит в GetApprovedMainMedia.
        var mainMediaIds = items
            .Where(x => x.MainMediaId.HasValue)
            .Select(x => x.MainMediaId!.Value)
            .ToList();
        var mainMediaByMediaId = mainMediaIds.Count == 0
            ? new Dictionary<Guid, MainMediaProjection>()
            : await deceasedRepository.GetApprovedMainMedia(mainMediaIds, cancellationToken);

        // F17.1: батчем резолвим имена авторов карточек (та же схема,
        // что и для авторов воспоминаний в F12) — иначе на странице
        // из 20 карточек был бы N+1.
        var creatorIds = items.Select(x => x.CreatedByUserId).Distinct().ToList();
        var creatorNames = creatorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await userRepository.GetDisplayNamesByIds(creatorIds, cancellationToken);

        var response = new PagedResponse<GetAllDeceasedItemResponse>
        {
            Items = items.Select(x =>
            {
                Guid? mediaId = null;
                string? bucket = null;
                string? storageKey = null;
                string? photoUrl = null;
                if (x.MainMediaId is { } mid
                    && mainMediaByMediaId.TryGetValue(mid, out var photo))
                {
                    mediaId = photo.Id;
                    bucket = photo.Bucket;
                    storageKey = photo.StorageKey;
                    // D36: оставляем для обратной совместимости со
                    // старыми клиентами. Новые клиенты используют
                    // MainPhotoBucket+MainPhotoStorageKey.
                    photoUrl = fileStorage.GetPublicUrl(photo.Bucket, photo.StorageKey);
                }

                return new GetAllDeceasedItemResponse
                {
                    Id = x.Id,
                    FullName = x.Name.FullName,
                    BirthDate = x.LifePeriod.BirthDate,
                    DeathDate = x.LifePeriod.DeathDate,
                    HasBurialLocation = x.BurialLocation is not null,
                    Latitude = x.BurialLocation?.Latitude,
                    Longitude = x.BurialLocation?.Longitude,
                    AccuracyMeters = x.BurialLocation?.AccuracyMeters,
                    Country = x.BurialLocation?.Country,
                    City = x.BurialLocation?.City,
                    CemeteryName = x.BurialLocation?.CemeteryName,
                    PlotNumber = x.BurialLocation?.PlotNumber,
                    GraveNumber = x.BurialLocation?.GraveNumber,
                    IsVerified = x.IsVerified,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedByUserName = creatorNames.GetValueOrDefault(x.CreatedByUserId),
                    MainMediaId = mediaId,
                    MainPhotoBucket = bucket,
                    MainPhotoStorageKey = storageKey,
                    MainPhotoUrl = photoUrl,
                };
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<GetAllDeceasedItemResponse>, Error>(response);
    }
}