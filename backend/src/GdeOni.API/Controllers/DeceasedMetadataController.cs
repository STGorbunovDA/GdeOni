using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления метаданными умерших.
/// </summary>
[Route("api/deceased-records")]
public class DeceasedMetadataController : ApiControllerBase
{
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
}