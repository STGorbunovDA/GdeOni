using GdeOni.API.Mappers;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.UseCase;
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
public sealed class DeceasedRecordsAdminController : ApiControllerBase
{
    /// <summary>
    /// Подтверждает карточку умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "SuperAdmin,Admin")]
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
    public async Task<IActionResult> Unverified(
        [FromRoute] Guid id,
        [FromServices] IUnverifiedDeceasedUseCase unverifiedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new UnverifyDeceasedCommand(id);
        var result = await unverifiedDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
    
    /// <summary>
    /// Одобряет фотографию карточки умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/photos/{photoId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApprovePhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid photoId,
        [FromServices] IApprovePhotoUseCase approvePhotoUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToApprovePhotoCommand(id, photoId);
        var result = await approvePhotoUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
    
    /// <summary>
    /// Отклоняет фотографию карточки умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/photos/{photoId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RejectPhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectPhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid photoId,
        [FromServices] IRejectPhotoUseCase rejectPhotoUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToRejectPhotoCommand(id, photoId);
        var result = await rejectPhotoUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
    
    /// <summary>
    /// Одобряет воспоминание.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/memories/{memoryId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApproveMemoryResponse>), StatusCodes.Status200OK)]
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
}