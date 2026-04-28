using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления воспоминаниями умерших.
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedMemoriesController : ApiControllerBase
{
    /// <summary>
    /// Добавляет воспоминание к карточке умершего.
    /// </summary>
    [HttpPost("{id:guid}/memories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddMemoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddMemory(
        [FromRoute] Guid id,
        [FromBody] AddMemoryRequest request,
        [FromServices] IAddMemoryUseCase addMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id);
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
    /// Удаляет воспоминание у карточки умершего.
    /// </summary>
    [HttpDelete("{id:guid}/memories/{memoryId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMemory(
        [FromRoute] Guid id,
        [FromRoute] Guid memoryId,
        [FromServices] IRemoveMemoryUseCase removeMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RemoveMemoryCommand(id, memoryId);
        var result = await removeMemoryUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}