using GdeOni.API.Response;
using GdeOni.Application.Complimentary.Commands.GrantToAll.Model;
using GdeOni.Application.Complimentary.Commands.GrantToAll.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// F17.6+. Массовая выдача комплиментарного доступа ВСЕМ пользователям —
/// один клик перед возвратом платного режима, чтобы никто резко не упёрся в
/// paywall. Только SuperAdmin (поштучная выдача — любому админу, а массовая
/// раздача доступа это операция уровня «включаю монетизацию»).
/// </summary>
[Tags("Admin")]
[Route("api/admin/complimentary-access")]
[Authorize(Roles = "SuperAdmin")]
public sealed class AdminBulkComplimentaryController : ApiControllerBase
{
    /// <summary>
    /// Выдать бесплатный доступ всем на N дней (по умолчанию 30). Только
    /// продлевает — у кого уже выдан более поздний срок, не трогает.
    /// </summary>
    [HttpPost("grant-all")]
    [ProducesResponseType(typeof(ApiResponse<GrantComplimentaryAccessToAllResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GrantAll(
        [FromBody] GrantComplimentaryToAllRequest? request,
        [FromServices] IGrantComplimentaryAccessToAllUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new GrantComplimentaryAccessToAllCommand(request?.DurationDays),
            cancellationToken);
        return FromResult(result);
    }
}

/// <summary>Тело запроса массовой выдачи. DurationDays = null → 30 дней.</summary>
public sealed record GrantComplimentaryToAllRequest(int? DurationDays);
