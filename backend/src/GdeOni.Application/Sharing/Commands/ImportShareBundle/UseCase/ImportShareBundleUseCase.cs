using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Commands.ImportShareBundle.UseCase;

/// <summary>
/// D46. Импортирует подборку в отслеживание текущего пользователя.
///
/// Каждая карточка добавляется с дефолтом (тип «Друг», напоминание о дне
/// памяти включено — как при добавлении из превью). Идемпотентно: уже
/// активно отслеживаемые пропускаются, приглушённые/архивные —
/// реактивируются. Подписочный гейт — на контроллере (импорт = точка
/// конверсии); новый юзер на триале импортирует сразу.
/// </summary>
public sealed class ImportShareBundleUseCase(
    IShareBundleRepository shareBundleRepository,
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IImportShareBundleUseCase
{
    public Task<Result<ImportShareBundleResponse, Error>> Execute(
        ImportShareBundleCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ImportShareBundleResponse, Error>> Handle(
        ImportShareBundleCommand command,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var bundle = await shareBundleRepository.GetByCode(command.Code, cancellationToken);
        if (bundle is null)
            return Errors.Share.NotFound();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (bundle.IsExpired(nowUtc))
            return Errors.Share.Expired();

        var user = await userRepository.GetByIdWithAllTracking(userIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userIdResult.Value);

        // Существующие карточки подборки (удалённые между шэром и импортом —
        // отпадают, попадут в Skipped).
        var existing = await deceasedRepository.GetForShare(bundle.DeceasedIds, cancellationToken);
        var existingIds = existing.Select(d => d.Id).ToHashSet();

        var added = 0;
        var skipped = 0;

        foreach (var deceasedId in bundle.DeceasedIds)
        {
            if (!existingIds.Contains(deceasedId))
            {
                skipped++;
                continue;
            }

            var wasActive = user.GetTracking(deceasedId) is { Status: TrackStatus.Active };

            var trackResult = user.TrackDeceased(
                deceasedId,
                RelationshipType.Friend,
                personalNotes: null,
                notifyOnDeathAnniversary: true,
                notifyOnBirthAnniversary: false);

            if (trackResult.IsFailure)
            {
                skipped++;
                continue;
            }

            if (wasActive)
                skipped++;
            else
                added++;
        }

        // Save только если реально что-то поменялось — не гоняем UPDATE зря.
        if (added > 0)
            await userRepository.Save(cancellationToken);

        return Result.Success<ImportShareBundleResponse, Error>(
            new ImportShareBundleResponse(added, skipped, bundle.DeceasedIds.Length));
    }
}
