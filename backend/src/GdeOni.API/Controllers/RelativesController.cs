using GdeOni.API.Authorization;
using GdeOni.API.Response;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.Model;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Функция «Родственники». По карточкам, которые отслеживает пользователь,
/// находит других отслеживающих с семейной/близкой связью и включённым
/// согласием. Почта не раскрывается — переписка внутренняя (Фаза 3).
///
/// <para>
/// BasicAuthenticated: доступно любому вошедшему (в т.ч. без подписки) — это
/// социальная функция связи между близкими, а не платный контент.
/// </para>
/// </summary>
[Route("api/relatives")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
[Tags("Relatives")]
public sealed class RelativesController : ApiControllerBase
{
    /// <summary>
    /// Список «родственников» текущего пользователя (считается вживую).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetMyRelativesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromServices] IGetMyRelativesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromResult(result);
    }
}
