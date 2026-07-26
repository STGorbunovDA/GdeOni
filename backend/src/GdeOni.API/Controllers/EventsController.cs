using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Events;
using GdeOni.API.Response;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Application.Events.Commands.SetHolidayReminder.UseCase;
using GdeOni.Application.Events.Queries.GetHolidays.Model;
using GdeOni.Application.Events.Queries.GetHolidays.UseCase;
using GdeOni.Application.Events.Queries.GetMyHolidayReminders.Model;
using GdeOni.Application.Events.Queries.GetMyHolidayReminders.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// События: справочник праздников/памятных дат по категориям
/// (поминальные, православные, мусульманские, государственные РФ).
/// Годовщины отслеживаемых умерших клиент считает сам из списка
/// tracked-deceased — отдельного эндпоинта для них нет.
/// </summary>
[Tags("Events")]
[Route("api/events")]
[Authorize]
public sealed class EventsController : ApiControllerBase
{
    /// <summary>
    /// Возвращает праздники в диапазоне дат (по умолчанию — сегодня…+30
    /// дней). Подвижные даты (Пасха, Радоница, мусульманские) считаются
    /// сервером. Диапазон ограничен 366 днями.
    /// </summary>
    [HttpGet("holidays")]
    [ProducesResponseType(typeof(ApiResponse<GetHolidaysResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHolidays(
        [FromQuery] GetHolidaysRequest request,
        [FromServices] IGetHolidaysUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToQuery(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Персональные настройки напоминаний о праздниках текущего пользователя.
    /// Возвращает только явно заданные настройки; дефолты (крупные → «в день»,
    /// мелкие → выключено) клиент считает сам по флагу IsMajor.
    /// </summary>
    [HttpGet("holiday-reminders")]
    [ProducesResponseType(typeof(ApiResponse<GetMyHolidayRemindersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyHolidayReminders(
        [FromServices] IGetMyHolidayRemindersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Задать/обновить напоминание о празднике. <c>leadDays</c> — набор «за
    /// сколько дней» (0 = в день, 1, 3, 7); пустой набор отключает напоминание.
    /// </summary>
    [HttpPut("holiday-reminders")]
    [ProducesResponseType(typeof(ApiResponse<SetHolidayReminderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetHolidayReminder(
        [FromBody] SetHolidayReminderRequest request,
        [FromServices] ISetHolidayReminderUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }
}
