using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.SetHolidayReminder.UseCase;

/// <summary>
/// Upsert настройки напоминания о празднике для текущего пользователя. Есть
/// запись по ключу — обновляем набор дней (no-op при тех же значениях), нет —
/// создаём. Пустой набор = отключить (для крупного праздника это перебьёт
/// дефолт «в день»).
/// </summary>
public sealed class SetHolidayReminderUseCase(
    ICurrentUserService currentUserService,
    IHolidayReminderRepository repository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : ISetHolidayReminderUseCase
{
    public Task<Result<SetHolidayReminderResponse, Error>> Execute(
        SetHolidayReminderCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<SetHolidayReminderResponse, Error>> Handle(
        SetHolidayReminderCommand command,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var userId = userIdResult.Value;
        var key = command.HolidayKey.Trim();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await repository.GetByUserAndKey(userId, key, cancellationToken);
        if (existing is null)
        {
            var reminder = HolidayReminder.Create(userId, key, command.LeadDays, nowUtc);
            await repository.Add(reminder, cancellationToken);
        }
        else
        {
            existing.SetLeadDays(command.LeadDays, nowUtc);
        }

        await repository.Save(cancellationToken);

        var normalized = command.LeadDays.Distinct().OrderBy(d => d).ToList();
        return Result.Success<SetHolidayReminderResponse, Error>(
            new SetHolidayReminderResponse(key, normalized));
    }
}
