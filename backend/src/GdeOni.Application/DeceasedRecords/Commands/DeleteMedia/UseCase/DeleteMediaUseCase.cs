using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.UseCase;

public sealed class DeleteMediaUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<DeleteMediaUseCase> logger)
    : IDeleteMediaUseCase
{
    public Task<UnitResult<Error>> Execute(
        DeleteMediaCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        DeleteMediaCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D7.47: filtered Include — грузим только одну media.
        var deceased = await deceasedRepository.GetByIdWithMediaById(
            command.DeceasedId, command.MediaId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var media = deceased.Media.FirstOrDefault(x => x.Id == command.MediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(command.MediaId);

        // D26. Удаление медиа разрешено только админам — наряду с
        // выкладкой и редактированием описания/main-photo. Контроллер уже
        // ограничен Roles=SuperAdmin,Admin; этот guard — вторая линия.
        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMedia.DeleteForbidden();

        var bucket = media.Bucket;
        var storageKey = media.StorageKey;

        var removeResult = deceased.RemoveMedia(command.MediaId);
        if (removeResult.IsFailure)
            return removeResult.Error;

        await deceasedRepository.Save(cancellationToken);

        try
        {
            await fileStorage.DeleteAsync(bucket, storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось удалить файл из MinIO после удаления метаданных. Bucket: {Bucket}, Key: {Key}",
                bucket,
                storageKey);
        }

        return UnitResult.Success<Error>();
    }
}
