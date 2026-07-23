using GdeOni.API.Response;
using GdeOni.Application.Admin.Queries.GetAdminStats.Model;
using GdeOni.Application.Admin.Queries.GetAdminStats.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// F38. Справка по системе для админа: сколько зарегистрировано людей,
/// сколько заведено карточек умерших, что с контентом, подписками и
/// обращениями. Только чтение, никаких действий.
/// </summary>
[ApiController]
[Tags("Admin")]
[Route("api/admin/stats")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AdminStatsController : ApiControllerBase
{
    /// <summary>
    /// Снимок счётчиков «здесь и сейчас». Параметров нет.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AdminStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(
        [FromServices] IGetAdminStatsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new GetAdminStatsQuery(), cancellationToken);
        return FromResult(result);
    }
}
