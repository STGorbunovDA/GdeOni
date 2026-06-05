using GdeOni.API.Authorization;
using GdeOni.API.Response;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D23. История платежей текущего пользователя. Whitelist'овая
/// ручка (BasicAuthenticated) — юзеру с истёкшей подпиской
/// должен быть доступен список платежей в Profile.
/// </summary>
[ApiController]
[Tags("Subscriptions")]
[Route("api/users/me/payments")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
public sealed class UserPaymentsController : ApiControllerBase
{
    /// <summary>
    /// Список платежей текущего пользователя. Отсортирован по
    /// <c>CreatedAtUtc DESC</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedPaymentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IGetMyPaymentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetMyPaymentsQuery(
            Page: page == 0 ? 1 : page,
            PageSize: pageSize == 0 ? 20 : pageSize);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }
}
