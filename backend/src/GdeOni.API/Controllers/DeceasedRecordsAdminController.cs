using GdeOni.API.Mappers;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления карточками умерших.
/// </summary>
[Route("api/deceased-records")]
[Tags("DeceasedRecords")]
public sealed class DeceasedRecordsAdminController : ApiControllerBase
{
    /// <summary>
    /// Подтверждает карточку умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<VerifyDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify(
        [FromRoute] Guid id,
        [FromServices] IVerifyDeceasedUseCase verifyDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new VerifyDeceasedCommand(id);
        var result = await verifyDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Снимает подтверждение с карточки умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/unverified")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<UnverifiedDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unverified(
        [FromRoute] Guid id,
        [FromServices] IUnverifiedDeceasedUseCase unverifiedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new UnverifiedDeceasedCommand(id);
        var result = await unverifiedDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
    
    /// <summary>
    /// Одобряет воспоминание.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/memories/{memoryId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApproveMemoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveMemory(
        [FromRoute] Guid id,
        [FromRoute] Guid memoryId,
        [FromServices] IApproveMemoryUseCase approveMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToApproveMemoryCommand(id, memoryId);
        var result = await approveMemoryUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
    
    /// <summary>
    /// Отклоняет воспоминание.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/memories/{memoryId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RejectMemoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectMemory(
        [FromRoute] Guid id,
        [FromRoute] Guid memoryId,
        [FromServices] IRejectMemoryUseCase rejectMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToRejectMemoryCommand(id, memoryId);
        var result = await rejectMemoryUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Одобряет медиафайл — переводит в ModerationStatus.Approved.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/media/{mediaId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveMedia(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] IApproveMediaModerationUseCase approveMediaModerationUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToApproveMediaModerationCommand(id, mediaId);
        var result = await approveMediaModerationUseCase.Execute(command, cancellationToken);

        return FromUnitResult(result);
    }

    /// <summary>
    /// Отклоняет медиафайл — переводит в ModerationStatus.Rejected и
    /// удаляет файл из MinIO (best-effort, см. D7.45). Если файл был
    /// помечен как главное фото — флаг сбрасывается.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/media/{mediaId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectMedia(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] IRejectMediaModerationUseCase rejectMediaModerationUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToRejectMediaModerationCommand(id, mediaId);
        var result = await rejectMediaModerationUseCase.Execute(command, cancellationToken);

        return FromUnitResult(result);
    }
}