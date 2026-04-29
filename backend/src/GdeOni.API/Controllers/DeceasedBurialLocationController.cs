using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления местом захоронения умершего.
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedBurialLocationController : ApiControllerBase
{
    /// <summary>
    /// Устанавливает место захоронения карточки умершего по GPS-координатам.
    /// Если место уже было задано — будет перезаписано новыми GPS-координатами.
    /// Доступно автору карточки и администраторам.
    /// </summary>
    [HttpPut("{id:guid}/burial-location/from-gps")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SetBurialLocationFromGpsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFromGps(
        [FromRoute] Guid id,
        [FromBody] SetBurialLocationFromGpsRequest request,
        [FromServices] ISetBurialLocationFromGpsUseCase setBurialLocationFromGpsUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id);
        var result = await setBurialLocationFromGpsUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}
