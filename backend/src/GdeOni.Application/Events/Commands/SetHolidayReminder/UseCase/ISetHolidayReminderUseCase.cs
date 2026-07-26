using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.SetHolidayReminder.UseCase;

public interface ISetHolidayReminderUseCase
{
    Task<Result<SetHolidayReminderResponse, Error>> Execute(
        SetHolidayReminderCommand command,
        CancellationToken cancellationToken);
}
