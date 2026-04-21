using GdeOni.API.Models;
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
using GdeOni.Domain.Shared;
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
    [HttpGet("{userId:guid}/trackings")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackedDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrackedDeceased(
        [FromRoute] Guid userId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackedDeceasedUseCase getTrackedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var accessDenied = EnsureUserResourceAccess(userId, currentUserService.UserId, isAdmin);
        if (accessDenied is not null)
            return accessDenied;

        var query = new GetTrackedDeceasedQuery(userId);
        var result = await getTrackedDeceasedUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Добавить умершего для отслеживания пользователю
    /// </summary>
    [HttpPost("{userId:guid}/trackings")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TrackDeceased(
        [FromRoute] Guid userId,
        [FromBody] TrackDeceasedRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ITrackDeceasedUseCase trackDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var accessDenied = EnsureUserResourceAccess(userId, currentUserService.UserId, isAdmin);
        if (accessDenied is not null)
            return accessDenied;

        var command = new TrackDeceasedCommand(
            userId,
            request.DeceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);

        var result = await trackDeceasedUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить отслеживание умершего для пользователя
    /// </summary>
    [HttpPut("{userId:guid}/trackings/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTracking(
        [FromRoute] Guid userId,
        [FromRoute] Guid deceasedId,
        [FromBody] UpdateTrackingRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IUpdateTrackingUseCase updateTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var accessDenied = EnsureUserResourceAccess(userId, currentUserService.UserId, isAdmin);
        if (accessDenied is not null)
            return accessDenied;

        var command = new UpdateTrackingCommand(
            userId,
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
    /// Получить конкретное отслеживание пользователя
    /// </summary>
    [HttpGet("{userId:guid}/trackings/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTracking(
        [FromRoute] Guid userId,
        [FromRoute] Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackingUseCase getTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var accessDenied = EnsureUserResourceAccess(userId, currentUserService.UserId, isAdmin);
        if (accessDenied is not null)
            return accessDenied;

        var query = new GetTrackingQuery(userId, deceasedId);
        var result = await getTrackingUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Полностью удалить отслеживание пользователя
    /// </summary>
    [HttpDelete("{userId:guid}/trackings/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTracking(
        [FromRoute] Guid userId,
        [FromRoute] Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IRemoveTrackingUseCase removeTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());
        var accessDenied = EnsureUserResourceAccess(userId, currentUserService.UserId, isAdmin);
        if (accessDenied is not null)
            return accessDenied;

        var command = new RemoveTrackingCommand(userId, deceasedId);
        var result = await removeTrackingUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}