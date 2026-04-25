using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления фотографиями умерших.
/// </summary>
[Route("api/deceased-records")]
public class DeceasedPhotosController : ApiControllerBase
{
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
        var command = request.ToCommand(id);
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

        return FromResult(result);
    }
}