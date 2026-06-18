using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;

public sealed class GetMyTrackedDeceasedDetailsUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMyTrackedDeceasedDetailsUseCase
{
    public Task<Result<GetMyTrackedDeceasedDetailsResult, Error>> Execute(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetMyTrackedDeceasedDetailsResult, Error>> Handle(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D7.55: filtered Include — грузим только tracking по DeceasedId.
        var user = await userRepository.GetByIdWithTrackingByDeceasedId(
            currentUserIdResult.Value, query.DeceasedId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        // E9.1 (mobile): архивный tracking больше не маскируем под NotTracked —
        // мобильный UI открывает архивные карточки, чтобы их можно было
        // восстановить через PATCH Status=Active. Скрытие архива от чужих
        // пользователей всё равно работает: GetTracking ищет только в
        // коллекции текущего user-а, чужие записи сюда не попадают.
        var tracking = user.GetTracking(query.DeceasedId);
        if (tracking is null)
            return Errors.Tracking.NotTracked();

        // D8.10: query-use-case → AsNoTracking-вариант.
        var deceased = await deceasedRepository.GetByIdWithMemoriesReadOnly(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        // Лекарство от N+1: главное фото отдельным узким SELECT'ом.
        // GetByIdWithMemoriesReadOnly Media не Include'ит, GetMainPhoto()
        // вернул бы null. Approved-фильтр зашит в GetApprovedMainMedia.
        Guid? mainMediaId = null;
        string? mainPhotoBucket = null;
        string? mainPhotoStorageKey = null;
        string? mainPhotoUrl = null;
        if (deceased.MainMediaId is { } mediaId)
        {
            var byId = await deceasedRepository.GetApprovedMainMedia(
                new[] { mediaId },
                cancellationToken);
            if (byId.TryGetValue(mediaId, out var photo))
            {
                mainMediaId = photo.Id;
                mainPhotoBucket = photo.Bucket;
                mainPhotoStorageKey = photo.StorageKey;
                // D36: оставляем для обратной совместимости со старыми
                // клиентами.
                mainPhotoUrl = fileStorage.GetPublicUrl(photo.Bucket, photo.StorageKey);
            }
        }

        // F12: имена авторов воспоминаний — batch'ем чтобы не словить N+1.
        var authorIds = deceased.Memories
            .Where(m => m.AuthorUserId.HasValue)
            .Select(m => m.AuthorUserId!.Value)
            .Distinct()
            .ToList();
        var authorNames = await userRepository.GetDisplayNamesByIds(authorIds, cancellationToken);

        // D14: модерация воспоминаний отключена — все воспоминания видны
        // всем. Параметр canSeeAllMemories в Result оставлен (используется
        // mapper'ом для совместимости), просто всегда true.
        return Result.Success<GetMyTrackedDeceasedDetailsResult, Error>(
            new GetMyTrackedDeceasedDetailsResult(
                deceased,
                tracking,
                CanSeeAllMemories: true,
                mainMediaId,
                mainPhotoBucket,
                mainPhotoStorageKey,
                mainPhotoUrl,
                authorNames));
    }
}
