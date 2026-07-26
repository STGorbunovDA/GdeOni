using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Queries.GetMyHolidayReminders.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetMyHolidayReminders.UseCase;

/// <summary>
/// Возвращает явные настройки напоминаний текущего пользователя. Без входных
/// данных — только по current-user, поэтому валидатор не нужен.
/// </summary>
public sealed class GetMyHolidayRemindersUseCase(
    ICurrentUserService currentUserService,
    IHolidayReminderRepository repository)
    : IGetMyHolidayRemindersUseCase
{
    public async Task<Result<GetMyHolidayRemindersResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var reminders = await repository.GetByUser(userIdResult.Value, cancellationToken);

        var items = reminders
            .Select(r => new HolidayReminderItem(r.HolidayKey, r.LeadDays))
            .ToList();

        return Result.Success<GetMyHolidayRemindersResponse, Error>(
            new GetMyHolidayRemindersResponse(items));
    }
}
