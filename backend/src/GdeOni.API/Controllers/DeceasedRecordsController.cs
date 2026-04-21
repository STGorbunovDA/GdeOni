using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления карточками умерших,
/// фотографиями, воспоминаниями и метаданными.
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedRecordsController : ApiControllerBase
{
    /// <summary>
    /// Создает новую карточку умершего.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = GetRequiredCurrentUserId(currentUserService);
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error.ToErrorResponse();

        var command = request.ToCreateCommand(currentUserIdResult.Value);
        var result = await createDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(
            result,
            value => value.ToCreatedResponse($"/api/deceased-records/{value.Id}"));
    }

    /// <summary>
    /// Возвращает список всех карточек умерших с пагинацией и фильтрацией.
    /// Доступно только администраторам.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DeceasedListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllDeceasedRequest request,
        [FromServices] IGetAllDeceasedUseCase getAllDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var result = await getAllDeceasedUseCase.Execute(query, cancellationToken);

        return FromResult(result, value => value.ToDto().ToOkResponse());
    }

    /// <summary>
    /// Возвращает карточку умершего по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IGetDeceasedByIdUseCase getDeceasedByIdUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetDeceasedByIdQuery(id);
        var result = await getDeceasedByIdUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Обновляет основную информацию карточки умершего.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDeceasedRequest request,
        [FromServices] IUpdateDeceasedUseCase updateDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id);
        var result = await updateDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Удаляет карточку умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IDeleteDeceasedUseCase deleteDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new DeleteDeceasedCommand(id);
        var result = await deleteDeceasedUseCase.Execute(command, cancellationToken);

        return FromUnitResult(result);
    }

    /// <summary>
    /// Добавляет фотографию к карточке умершего.
    /// </summary>
    [HttpPost("{id:guid}/photos")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddPhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddPhoto(
        [FromRoute] Guid id,
        [FromBody] AddPhotoRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IAddPhotoUseCase addPhotoUseCase,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = GetRequiredCurrentUserId(currentUserService);
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error.ToErrorResponse();

        var command = request.ToCommand(id, currentUserIdResult.Value);
        var result = await addPhotoUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Обновляет фотографию карточки умершего.
    /// </summary>
    [HttpPut("{id:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdatePhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid photoId,
        [FromBody] UpdatePhotoRequest request,
        [FromServices] IUpdatePhotoUseCase updatePhotoUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, photoId);
        var result = await updatePhotoUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Удаляет фотографию у карточки умершего.
    /// </summary>
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid photoId,
        [FromServices] IRemovePhotoUseCase removePhotoUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RemovePhotoCommand(id, photoId);
        var result = await removePhotoUseCase.Execute(command, cancellationToken);

        return FromUnitResult(result);
    }

    /// <summary>
    /// Делает указанную фотографию основной.
    /// </summary>
    [HttpPut("{id:guid}/photos/{photoId:guid}/primary")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SetPrimaryPhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPrimaryPhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid photoId,
        [FromServices] ISetPrimaryPhotoUseCase setPrimaryPhotoUseCase,
        CancellationToken cancellationToken)
    {
        var command = DeceasedRecordsMapping.ToCommand(id, photoId);
        var result = await setPrimaryPhotoUseCase.Execute(command, cancellationToken);

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
    /// Добавляет воспоминание к карточке умершего.
    /// </summary>
    [HttpPost("{id:guid}/memories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddMemoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddMemory(
        [FromRoute] Guid id,
        [FromBody] AddMemoryRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IAddMemoryUseCase addMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = GetRequiredCurrentUserId(currentUserService);
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error.ToErrorResponse();

        var command = request.ToCommand(id, currentUserIdResult.Value);
        var result = await addMemoryUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Обновляет воспоминание у карточки умершего.
    /// </summary>
    [HttpPut("{id:guid}/memories/{memoryId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMemoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMemory(
        [FromRoute] Guid id,
        [FromRoute] Guid memoryId,
        [FromBody] UpdateMemoryRequest request,
        [FromServices] IUpdateMemoryUseCase updateMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, memoryId);
        var result = await updateMemoryUseCase.Execute(command, cancellationToken);

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

    /// <summary>
    /// Удаляет воспоминание у карточки умершего.
    /// </summary>
    [HttpDelete("{id:guid}/memories/{memoryId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMemory(
        [FromRoute] Guid id,
        [FromRoute] Guid memoryId,
        [FromServices] IRemoveMemoryUseCase removeMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RemoveMemoryCommand(id, memoryId);
        var result = await removeMemoryUseCase.Execute(command, cancellationToken);

        return FromUnitResult(result);
    }

    /// <summary>
    /// Обновляет метаданные карточки умершего.
    /// </summary>
    [HttpPut("{id:guid}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMetadataResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMetadata(
        [FromRoute] Guid id,
        [FromBody] UpdateMetadataRequest request,
        [FromServices] IUpdateMetadataUseCase updateMetadataUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id);
        var result = await updateMetadataUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Очищает метаданные карточки умершего.
    /// </summary>
    [HttpDelete("{id:guid}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ClearMetadataResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearMetadata(
        [FromRoute] Guid id,
        [FromServices] IClearMetadataUseCase clearMetadataUseCase,
        CancellationToken cancellationToken)
    {
        var command = new ClearMetadataCommand(id);
        var result = await clearMetadataUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

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
    /// Возвращает расстояние от переданных координат до места захоронения.
    /// </summary>
    [HttpGet("{id:guid}/distance")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetDistanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistance(
        [FromRoute] Guid id,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] IGetDistanceUseCase getDistanceUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetDistanceQuery(id, latitude, longitude);
        var result = await getDistanceUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Возвращает возраст на момент смерти.
    /// </summary>
    [HttpGet("{id:guid}/age-at-death")]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgeAtDeath(
        [FromRoute] Guid id,
        [FromServices] IGetAgeAtDeathUseCase getAgeAtDeathUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAgeAtDeathUseCase.Execute(new GetAgeAtDeathQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Проверяет, есть ли у карточки фотографии.
    /// </summary>
    [HttpGet("{id:guid}/has-photos")]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasPhotos(
        [FromRoute] Guid id,
        [FromServices] IHasPhotosUseCase hasPhotosUseCase,
        CancellationToken cancellationToken)
    {
        var query = new HasPhotosQuery(id);
        var result = await hasPhotosUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Проверяет, есть ли у карточки воспоминания.
    /// </summary>
    [HttpGet("{id:guid}/has-memories")]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasMemories(
        [FromRoute] Guid id,
        [FromServices] IHasMemoriesUseCase hasMemoriesUseCase,
        CancellationToken cancellationToken)
    {
        var query = new HasMemoriesQuery(id);
        var result = await hasMemoriesUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }
}