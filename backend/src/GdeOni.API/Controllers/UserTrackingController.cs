using GdeOni.API.Extensions;
using GdeOni.API.Models.Users;
using GdeOni.API.Response;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Application.Users.Commands.RemoveTracking.UseCase;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Application.Users.Commands.TrackDeceased.UseCase;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Application.Users.Commands.UpdateTracking.UseCase;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.UseCase;
using GdeOni.Application.Users.Queries.GetTracking.Model;
using GdeOni.Application.Users.Queries.GetTracking.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления отслеживанием умерших пользователем.
/// </summary>
[Route("api/users/tracking")]
public sealed class UserTrackingController : ApiControllerBase
{
    /// <summary>
    /// Возвращает список отслеживаемых умерших для указанного пользователя.
    /// Доступен владельцу данных.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackedDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrackedDeceased(
        [FromServices] IGetTrackedDeceasedUseCase getTrackedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
       
        var result = await getTrackedDeceasedUseCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Добавляет умершего в список отслеживаемых пользователем.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TrackDeceased(
        [FromBody] TrackDeceasedRequest request,
        [FromServices] ITrackDeceasedUseCase trackDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new TrackDeceasedCommand(
            request.DeceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);

        var result = await trackDeceasedUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновляет настройки отслеживания умершего.
    /// Доступен владельцу данных.
    /// </summary>
    [HttpPut("{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTracking(
        [FromRoute] Guid deceasedId,
        [FromBody] UpdateTrackingRequest request,
        [FromServices] IUpdateTrackingUseCase updateTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTrackingCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary,
            request.TrackStatus);

        var result = await updateTrackingUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Возвращает конкретную запись отслеживания умершего.
    /// Доступен владельцу данных.
    /// </summary>
    [HttpGet("{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTracking(
        [FromRoute] Guid deceasedId,
        [FromServices] IGetTrackingUseCase getTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetTrackingQuery(deceasedId);
        var result = await getTrackingUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Удаляет запись отслеживания умершего.
    /// Доступен владельцу данных.
    /// </summary>
    [HttpDelete("{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTracking(
        [FromRoute] Guid deceasedId,
        [FromServices] IRemoveTrackingUseCase removeTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTrackingCommand(deceasedId);
        var result = await removeTrackingUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}