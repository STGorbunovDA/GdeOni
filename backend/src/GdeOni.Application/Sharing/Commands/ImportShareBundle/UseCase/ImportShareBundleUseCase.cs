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
/// Добавляем СТРОГО тех, кого у получателя нет (по бизнес-логике владельца):
///   - записи нет вообще (никогда не отслеживал ИЛИ раньше удалил запись) —
///     добавляем с типом «Другое», напоминанием о дне памяти, а если у
///     умершего указана дата рождения — и напоминанием о дне рождения;
///   - запись есть при ЛЮБОМ статусе (Active/Muted/Archived) — не трогаем:
///     архив оставляем архивом, настройки активных не перезаписываем,
///     считаем «уже есть» (Skipped);
///   - карточку удалили из системы — добавлять нечего (Skipped).
///
/// Подписочный гейт — на контроллере (импорт = точка конверсии); новый юзер
/// на триале импортирует сразу.
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
        // отпадают, попадут в Skipped). Индекс по id: нужен и для проверки
        // наличия, и чтобы взять дату рождения (для напоминания о ДР).
        var existing = await deceasedRepository.GetForShare(bundle.DeceasedIds, cancellationToken);
        var byId = existing.ToDictionary(d => d.Id);

        var added = 0;
        var skipped = 0;

        foreach (var deceasedId in bundle.DeceasedIds)
        {
            if (!byId.TryGetValue(deceasedId, out var deceased))
            {
                // Карточку удалили из системы между шэром и импортом — нечего добавлять.
                skipped++;
                continue;
            }

            // Запись есть при любом статусе (active/muted/archived) — не трогаем.
            // Архив оставляем архивом, активным не перезаписываем настройки.
            if (user.GetTracking(deceasedId) is not null)
            {
                skipped++;
                continue;
            }

            // Записи нет (никогда не отслеживал ИЛИ раньше удалил) — добавляем как «Другое».
            // Напоминание о дне памяти включаем всегда; о дне рождения — только
            // если дата рождения указана (иначе напоминать не о чем).
            var trackResult = user.TrackDeceased(
                deceasedId,
                RelationshipType.Other,
                personalNotes: null,
                notifyOnDeathAnniversary: true,
                notifyOnBirthAnniversary: deceased.LifePeriod.BirthDate is not null);

            if (trackResult.IsFailure)
            {
                skipped++;
                continue;
            }

            added++;
        }

        // Save только если реально что-то поменялось — не гоняем UPDATE зря.
        if (added > 0)
            await userRepository.Save(cancellationToken);

        return Result.Success<ImportShareBundleResponse, Error>(
            new ImportShareBundleResponse(added, skipped, bundle.DeceasedIds.Length));
    }
}
