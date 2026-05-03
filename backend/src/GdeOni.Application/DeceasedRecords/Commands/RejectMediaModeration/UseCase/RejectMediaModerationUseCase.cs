using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.UseCase;

public sealed class RejectMediaModerationUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<RejectMediaModerationUseCase> logger)
    : IRejectMediaModerationUseCase
{
    public Task<UnitResult<Error>> Execute(
        RejectMediaModerationCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        RejectMediaModerationCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMedia.ModerationForbidden();

        // D7.47: filtered Include — грузим только одну media.
        var deceased = await deceasedRepository.GetByIdWithMediaById(
            command.DeceasedId, command.MediaId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        // Захватываем bucket/storageKey ДО Reject, чтобы потом удалить
        // файл из MinIO. Без этого Rejected-фото остаётся публично
        // доступным по прямому URL до следующего orphan-cleanup
        // (≤24h). См. D7.45.
        var media = deceased.Media.FirstOrDefault(x => x.Id == command.MediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(command.MediaId);

        var bucket = media.Bucket;
        var storageKey = media.StorageKey;

        var result = deceased.RejectMedia(command.MediaId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        // Best-effort, как в DeleteMediaUseCase / UploadMedia rollback.
        // Если MinIO упал — DB уже зафиксировала Rejected, а файл
        // подберёт MinioOrphanCleanupService (D7.27).
        try
        {
            await fileStorage.DeleteAsync(bucket, storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось удалить отклонённый файл из MinIO. Bucket: {Bucket}, Key: {Key}",
                bucket,
                storageKey);
        }

        return UnitResult.Success<Error>();
    }
}
