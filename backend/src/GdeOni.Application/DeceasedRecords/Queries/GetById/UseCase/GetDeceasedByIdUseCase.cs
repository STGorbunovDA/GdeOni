using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;

public sealed class GetDeceasedByIdUseCase(
    IDeceasedRepository deceasedRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetDeceasedByIdUseCase
{
    public Task<Result<GetDeceasedByIdResult, Error>> Execute(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetDeceasedByIdResult, Error>> Handle(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D8.10: query-use-case → AsNoTracking-вариант.
        var deceased = await deceasedRepository.GetByIdWithMemoriesReadOnly(query.Id, cancellationToken);

        if (deceased is null)
            return Errors.General.NotFound("deceased", query.Id);

        // Лекарство от N+1: тянем главное фото отдельным узким SELECT'ом,
        // только если MainMediaId есть. GetByIdWithMemoriesReadOnly не
        // Include'ит Media, поэтому Deceased.GetMainPhoto() здесь вернул
        // бы null. Фильтр Approved живёт внутри GetApprovedMainMedia.
        Guid? mainMediaId = null;
        string? mainPhotoUrl = null;
        if (deceased.MainMediaId is { } mediaId)
        {
            var byId = await deceasedRepository.GetApprovedMainMedia(
                new[] { mediaId },
                cancellationToken);
            if (byId.TryGetValue(mediaId, out var photo))
            {
                mainMediaId = photo.Id;
                mainPhotoUrl = fileStorage.GetPublicUrl(photo.Bucket, photo.StorageKey);
            }
        }

        // D14: модерация воспоминаний отключена — все воспоминания видны
        // всем. Параметр canSeeAllMemories оставлен в Result для
        // совместимости с mapper'ом, всегда true.
        return Result.Success<GetDeceasedByIdResult, Error>(
            new GetDeceasedByIdResult(deceased, CanSeeAllMemories: true, mainMediaId, mainPhotoUrl));
    }
}
