using GdeOni.API.Models.Relatives;
using GdeOni.API.Response;
using GdeOni.Application.Relatives.Commands.ResolveRelativeReport.Model;
using GdeOni.Application.Relatives.Commands.ResolveRelativeReport.UseCase;
using GdeOni.Application.Relatives.Queries.GetRelativeReports.Model;
using GdeOni.Application.Relatives.Queries.GetRelativeReports.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Функция «Родственники» (Фаза 5): модерация жалоб. Только для админов.
/// Блокировка нарушителя — существующий эндпоинт /api/users/{id}/block
/// (User.Block автоматически убирает заблокированного из всей функции).
/// </summary>
[Route("api/admin/relative-reports")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Tags("Relatives")]
public sealed class RelativesAdminController : ApiControllerBase
{
    /// <summary>
    /// Список жалоб. По умолчанию только неразобранные (очередь модерации);
    /// <c>pendingOnly=false</c> — вся история.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetRelativeReportsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReports(
        [FromQuery] bool pendingOnly,
        [FromServices] IGetRelativeReportsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new GetRelativeReportsQuery(pendingOnly), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Пометить жалобу разобранной (с необязательной пометкой о решении).
    /// Идемпотентно: повторный вызов на уже разобранной — 200 без изменений.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(
        [FromRoute] Guid id,
        [FromBody] ResolveRelativeReportRequest request,
        [FromServices] IResolveRelativeReportUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new ResolveRelativeReportCommand(id, request.Note), cancellationToken);
        return FromUnitResult(result);
    }
}
