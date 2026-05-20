using GdeOni.API.Response;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Aggregates.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D23. Админ-история платежей для будущей web-админки (F17.8).
/// Сейчас доступна через swagger / curl для ручного аудита.
/// </summary>
[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AdminPaymentsController : ApiControllerBase
{
    /// <summary>
    /// Пагинированный список платежей с фильтрами. Все query-параметры
    /// опциональны.
    /// </summary>
    /// <param name="userId">Фильтр по конкретному юзеру.</param>
    /// <param name="status">Pending / Succeeded / Cancelled / Failed.</param>
    /// <param name="createdFromUtc">Нижняя граница CreatedAtUtc (включительно).</param>
    /// <param name="createdToUtc">Верхняя граница CreatedAtUtc (включительно).</param>
    /// <param name="page">Номер страницы, &gt;= 1.</param>
    /// <param name="pageSize">Размер страницы, 1..100.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedPaymentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? userId,
        [FromQuery] PaymentRecordStatus? status,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IGetAdminPaymentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminPaymentsQuery(
            UserId: userId,
            Status: status,
            CreatedFromUtc: createdFromUtc,
            CreatedToUtc: createdToUtc,
            Page: page == 0 ? 1 : page,
            PageSize: pageSize == 0 ? 20 : pageSize);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }
}
