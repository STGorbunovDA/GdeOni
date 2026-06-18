using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.UseCase;

/// <summary>
/// F13 (web). Прокси-скачивание медиа через сервер. Нужен, потому что
/// presigned URL'ы MinIO в dev-конфиге используют host
/// <c>10.0.2.2:9000</c> для Android-эмулятора — браузер на Windows этот
/// host не разрешает. Web-клиент дёргает этот эндпоинт, бэк сам
/// проксирует поток из MinIO, клиент получает PDF и открывает его
/// (через blob URL → window.open).
///
/// Логика проверки прав — дубликат GetMediaByIdUseCase: D11.13.1 для
/// Pending/Rejected. Дублирование осознанное — это узкий запрос на
/// чтение файла, не хочу тянуть весь MediaDetailsResponse только ради
/// проверки.
/// </summary>
public sealed class DownloadMediaUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService)
    : IDownloadMediaUseCase
{
    public async Task<Result<DownloadMediaResult, Error>> Execute(
        DownloadMediaQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var deceased = await deceasedRepository.GetByIdWithMediaById(
            query.DeceasedId, query.MediaId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        var media = deceased.Media.FirstOrDefault(x => x.Id == query.MediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(query.MediaId);

        if (media.ModerationStatus != ModerationStatus.Approved)
        {
            var currentUserId = currentUserIdResult.Value;
            var canSeePending = currentUserService.IsAdmin()
                || media.UploadedByUserId == currentUserId
                || deceased.CreatedByUserId == currentUserId;
            if (!canSeePending)
                return Errors.DeceasedMedia.NotFound(query.MediaId);
        }

        var downloaded = await fileStorage.DownloadAsync(
            media.Bucket, media.StorageKey, cancellationToken);

        return Result.Success<DownloadMediaResult, Error>(
            new DownloadMediaResult(downloaded, media.OriginalFileName));
    }
}
