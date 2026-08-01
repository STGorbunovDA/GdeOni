using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.UseCase;

/// <summary>
/// D47. «Вахтёр» фото. Прокси-стрим файла через сервер только для
/// авторизованного пользователя — заменяет вечные публичные ссылки MinIO
/// для фото (утёкший URL без входа больше не откроется, бакеты приватны).
///
/// Проверка прав — зеркало <c>DownloadMediaUseCase</c> (D11.13.1):
/// Pending/Rejected видит только админ, автор карточки или загрузивший.
/// Дублирование осознанное — узкий запрос на чтение файла по одному
/// mediaId, тянуть весь <c>MediaDetailsResponse</c> ради проверки не нужно.
///
/// Уровень доступа — BasicAuthenticated (см. контроллер): достаточно
/// просто входа. Данные карточки (сам факт, что media существует и её id)
/// клиент узнаёт из data-эндпоинтов, у которых свой гейт; байты фото не
/// чувствительнее самой карточки, поэтому подписку здесь не требуем — иначе
/// у не-подписчика в basic-authenticated контексте не грузились бы превью.
/// </summary>
public sealed class GetMediaContentUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService)
    : IGetMediaContentUseCase
{
    public async Task<Result<GetMediaContentResult, Error>> Execute(
        GetMediaContentQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var info = await deceasedRepository.GetMediaContentInfoById(
            query.MediaId, cancellationToken);
        if (info is null)
            return Errors.DeceasedMedia.NotFound(query.MediaId);

        if (info.ModerationStatus != ModerationStatus.Approved)
        {
            var currentUserId = currentUserIdResult.Value;
            var canSeePending = currentUserService.IsAdmin()
                || info.UploadedByUserId == currentUserId
                || info.DeceasedCreatedByUserId == currentUserId;
            if (!canSeePending)
                return Errors.DeceasedMedia.NotFound(query.MediaId);
        }

        var downloaded = await fileStorage.DownloadAsync(
            info.Bucket, info.StorageKey, cancellationToken);

        return Result.Success<GetMediaContentResult, Error>(
            new GetMediaContentResult(downloaded, info.OriginalFileName));
    }
}
