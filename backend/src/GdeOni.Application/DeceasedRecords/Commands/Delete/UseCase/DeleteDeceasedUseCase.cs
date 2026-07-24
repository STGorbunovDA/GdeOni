using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;

public sealed class DeleteDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<DeleteDeceasedUseCase> logger)
    : IDeleteDeceasedUseCase
{
    public Task<Result<DeleteDeceasedResponse, Error>> Execute(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<DeleteDeceasedResponse, Error>> Handle(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.Deceased.DeleteForbidden();

        var deceased = await deceasedRepository.GetByIdWithMedia(command.Id, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.Id);

        var mediaToDelete = deceased.Media
            .Select(m => (m.Bucket, m.StorageKey))
            .ToArray();

        // Каскадный DELETE на стороне БД (не Remove+Save): обходит цикл
        // MainMediaId ↔ deceased_id в EF, из-за которого удаление карточки
        // с главным фото падало с circular dependency. См. DeleteById.
        await deceasedRepository.DeleteById(command.Id, cancellationToken);

        // Best-effort: БД уже зафиксирована, файлы в MinIO теперь сироты.
        // Если удаление здесь упадёт — фоновый MinioOrphanCleanupService
        // подметёт их позже.
        foreach (var (bucket, key) in mediaToDelete)
        {
            try
            {
                await fileStorage.DeleteAsync(bucket, key, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось удалить файл из MinIO после удаления Deceased {DeceasedId}. Bucket={Bucket}, Key={Key}",
                    command.Id, bucket, key);
            }
        }

        return Result.Success<DeleteDeceasedResponse, Error>(
            new DeleteDeceasedResponse(true));
    }
}
