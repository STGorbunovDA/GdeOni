using GdeOni.API.Models;
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
/// Отслеживание умерших
/// </summary>
[Route("api/users")]
public sealed class UserTrackingController : ApiControllerBase
{
    /// <summary>
    /// Получить отслеживаемых умерших у пользователя
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="currentUserService"></param>
    /// <param name="getTrackedDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{userId:guid}/trackings")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackedDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetTrackedDeceasedResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrackedDeceased(
        [FromRoute] Guid userId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackedDeceasedUseCase getTrackedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(userId, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await getTrackedDeceasedUseCase.Execute(new GetTrackedDeceasedQuery(userId), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Добавить умершего для отслеживания пользователю
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="dto"></param>
    /// <param name="currentUserService"></param>
    /// <param name="trackDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{userId:guid}/trackings")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TrackDeceased(
        [FromRoute] Guid userId,
        [FromBody] TrackDeceasedDto dto,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ITrackDeceasedUseCase trackDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(userId, currentUserService.UserId, isAdmin))
            return Forbid();

        var command = new TrackDeceasedCommand(userId, dto.DeceasedId, dto.RelationshipType, dto.PersonalNotes,
            dto.NotifyOnDeathAnniversary, dto.NotifyOnBirthAnniversary);
        var result = await trackDeceasedUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить отслеживание умершего для пользователя
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="deceasedId"></param>
    /// <param name="dto"></param>
    /// <param name="currentUserService"></param>
    /// <param name="updateTrackingUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{userId:guid}/trackings/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTracking(
        [FromRoute] Guid userId,
        [FromRoute] Guid deceasedId,
        [FromBody] UpdateTrackingDto dto,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IUpdateTrackingUseCase updateTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(userId, currentUserService.UserId, isAdmin))
            return Forbid();

        var command = new UpdateTrackingCommand(userId, deceasedId, dto.RelationshipType, dto.PersonalNotes,
            dto.NotifyOnDeathAnniversary, dto.NotifyOnBirthAnniversary,
            dto.TrackStatus);

        var result = await updateTrackingUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Получить конкретное отслеживание пользователя
    /// </summary>
    [HttpGet("{id:guid}/trackings/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTracking(
        [FromRoute] Guid id,
        [FromRoute] Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackingUseCase getTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await getTrackingUseCase.Execute(new GetTrackingQuery(id, deceasedId), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Полностью удалить отслеживание пользователя
    /// </summary>
    [HttpDelete("{id:guid}/trackings/{deceasedId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTracking(
        [FromRoute] Guid id,
        [FromRoute] Guid deceasedId,
        [FromServices] IRemoveTrackingUseCase removeTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var result = await removeTrackingUseCase.Execute(
            new RemoveTrackingCommand(id, deceasedId), cancellationToken);
        return FromResult(result);
    }
}