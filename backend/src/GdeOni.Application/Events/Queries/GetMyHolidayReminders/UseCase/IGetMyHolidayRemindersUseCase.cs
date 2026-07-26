using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Queries.GetMyHolidayReminders.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetMyHolidayReminders.UseCase;

public interface IGetMyHolidayRemindersUseCase
{
    Task<Result<GetMyHolidayRemindersResponse, Error>> Execute(CancellationToken cancellationToken);
}
