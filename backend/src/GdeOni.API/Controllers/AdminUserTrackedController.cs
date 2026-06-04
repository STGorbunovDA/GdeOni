using GdeOni.API.Response;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.UseCase;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Админ-эндпоинты для управления отслеживаниями конкретного юзера:
/// просмотр всего списка, снятие одного отслеживания, снятие всех разом.
/// </summary>
[ApiController]
[Route("api/admin/users/{userId:guid}/tracked-deceased")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Tags("Admin")]
public sealed class AdminUserTrackedController : ApiControllerBase
{
    /// <summary>Список отслеживаний юзера с пагинацией.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetUserTrackedDeceasedForAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        Guid userId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IGetUserTrackedDeceasedForAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetUserTrackedDeceasedForAdminQuery(
            userId,
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 50 : pageSize);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>Снять одно отслеживание у юзера.</summary>
    [HttpDelete("{deceasedId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveOne(
        Guid userId,
        Guid deceasedId,
        [FromServices] IAdminRemoveUserTrackingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new AdminRemoveUserTrackingCommand(userId, deceasedId),
            cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>Снять все отслеживания у юзера. Возвращает количество удалённых.</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<AdminRemoveAllUserTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAll(
        Guid userId,
        [FromServices] IAdminRemoveAllUserTrackingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new AdminRemoveAllUserTrackingCommand(userId),
            cancellationToken);
        return FromResult(result);
    }
}
