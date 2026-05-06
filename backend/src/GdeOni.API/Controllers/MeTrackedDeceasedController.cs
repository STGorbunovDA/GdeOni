using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Routing;
using GdeOni.API.Models.Users;
using GdeOni.API.Response;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Application.Users.Commands.RemoveTracking.UseCase;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Application.Users.Commands.TrackDeceased.UseCase;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Application.Users.Commands.UpdateTracking.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.UseCase;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Application.Users.Queries.IsTrackedByMe.UseCase;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Список отслеживаемых умерших текущего пользователя (через JWT).
/// </summary>
[Route("api/users/me/tracked-deceased")]
[Authorize]
public sealed class MeTrackedDeceasedController : ApiControllerBase
{
    /// <summary>
    /// Возвращает список умерших, которых отслеживает текущий пользователь, с пагинацией.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MyTrackedDeceasedListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetMyTrackedDeceasedRequest request,
        [FromServices] IGetMyTrackedDeceasedListUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetMyTrackedDeceasedListQuery(request.Page, request.PageSize);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Возвращает детальную карточку отслеживаемого умершего.
    /// Если текущий пользователь не отслеживает этого умершего — 403.
    /// </summary>
    [HttpGet("{deceasedId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MyTrackedDeceasedDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        [FromRoute] Guid deceasedId,
        [FromServices] IGetMyTrackedDeceasedDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetMyTrackedDeceasedDetailsQuery(deceasedId);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result, r => r.ToDetailsResponse().ToOkResponse());
    }

    /// <summary>
    /// Возвращает true/false — отслеживает ли текущий пользователь этого умершего.
    /// Используется для UI-кнопки «Отслеживать».
    /// </summary>
    [HttpGet("{deceasedId:guid}/exists")]
    [ProducesResponseType(typeof(ApiResponse<IsTrackedByMeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Exists(
        [FromRoute] Guid deceasedId,
        [FromServices] IIsTrackedByMeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new IsTrackedByMeQuery(deceasedId);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Добавляет существующего умершего в список отслеживаемых текущего пользователя.
    /// Идемпотентен: повторный вызов на уже существующем tracking (Active/Muted/Archived)
    /// переводит его в Active и обновляет настройки.
    /// </summary>
    [HttpPost("{deceasedId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Track(
        [FromRoute] Guid deceasedId,
        [FromBody] AddMeTrackingRequest request,
        [FromServices] ITrackDeceasedUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = new TrackDeceasedCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);

        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновляет настройки отслеживания умершего у текущего пользователя.
    /// </summary>
    [HttpPatch("{deceasedId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid deceasedId,
        [FromBody] UpdateTrackingRequest request,
        [FromServices] IUpdateTrackingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTrackingCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary,
            request.TrackStatus);

        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Убирает умершего из списка отслеживаемых текущего пользователя.
    /// </summary>
    [HttpDelete("{deceasedId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Untrack(
        [FromRoute] Guid deceasedId,
        [FromServices] IRemoveTrackingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTrackingCommand(deceasedId);
        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Возвращает координаты могилы и deep-link'и (Yandex / Google / 2GIS)
    /// для построения маршрута от переданных координат пользователя.
    /// Доступно только tracker'у этой карточки. Если у умершего нет
    /// координат места захоронения — 409.
    /// </summary>
    [HttpGet("{deceasedId:guid}/route")]
    [ProducesResponseType(typeof(ApiResponse<RouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetRoute(
        [FromRoute] Guid deceasedId,
        [FromQuery] double fromLat,
        [FromQuery] double fromLon,
        [FromQuery] RoutingMode mode,
        [FromServices] IGetRouteToGraveUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetRouteToGraveQuery(deceasedId, fromLat, fromLon, mode);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result, r => r.ToRouteResponse().ToOkResponse());
    }
}
