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
using GdeOni.Application.Events.Queries.GetMyCustomEvents.Model;
using GdeOni.Application.Events.Queries.GetMyCustomEvents.UseCase;
using GdeOni.Application.Events.Commands.CreateCustomEvent.Model;
using GdeOni.Application.Events.Commands.CreateCustomEvent.UseCase;
using GdeOni.Application.Events.Commands.UpdateCustomEvent.Model;
using GdeOni.Application.Events.Commands.UpdateCustomEvent.UseCase;
using GdeOni.Application.Events.Commands.DeleteCustomEvent.UseCase;
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

    // ─────────────── Ручные (пользовательские) события ───────────────

    /// <summary>Список ручных событий текущего пользователя (приватные).</summary>
    [HttpGet("custom")]
    [ProducesResponseType(typeof(ApiResponse<GetMyCustomEventsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyCustomEvents(
        [FromServices] IGetMyCustomEventsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Создать ручное событие (например, «ДР друга»). Повторяется каждый год по
    /// дню/месяцу; напоминания — тот же набор «за сколько дней», что у праздников.
    /// </summary>
    [HttpPost("custom")]
    [ProducesResponseType(typeof(ApiResponse<CreateCustomEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCustomEvent(
        [FromBody] CustomEventRequest request,
        [FromServices] ICreateCustomEventUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new CreateCustomEventCommand(request.Title, request.Date, request.LeadDays),
            cancellationToken);
        return FromResult(result);
    }

    /// <summary>Обновить своё ручное событие.</summary>
    [HttpPut("custom/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomEvent(
        [FromRoute] Guid id,
        [FromBody] CustomEventRequest request,
        [FromServices] IUpdateCustomEventUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new UpdateCustomEventCommand(id, request.Title, request.Date, request.LeadDays),
            cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>Удалить своё ручное событие.</summary>
    [HttpDelete("custom/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomEvent(
        [FromRoute] Guid id,
        [FromServices] IDeleteCustomEventUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(id, cancellationToken);
        return FromUnitResult(result);
    }
}
