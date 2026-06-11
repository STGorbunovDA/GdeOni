using GdeOni.API.Mappers;
using GdeOni.API.Models.Admin;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D24/F17.9. Глобальная админ-лента правок карточек умерших — не привязана
/// к конкретной карточке (в отличие от /api/deceased-records/{id}/edits).
/// Для мобильной/web админки вкладки "История правок".
/// </summary>
[ApiController]
[Route("api/admin/edits")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Tags("Admin")]
public sealed class AdminEditsController : ApiControllerBase
{
    /// <summary>
    /// Все правки по системе с пагинацией. Сортировка по EditedAtUtc desc.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetAllEditsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllEditsRequest request,
        [FromServices] IGetAllEditsUseCase useCase,
        CancellationToken cancellationToken)
    {
        // Маппер собирает Query без inline clamping — пагинация и
        // диапазон валидируются в GetAllEditsQueryValidator (400 на
        // невалидные значения вместо silent normalization).
        var result = await useCase.Execute(request.ToQuery(), cancellationToken);
        return FromResult(result);
    }
}
