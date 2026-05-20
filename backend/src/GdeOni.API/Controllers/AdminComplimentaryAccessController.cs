using GdeOni.API.Models.Admin.ComplimentaryAccess;
using GdeOni.API.Response;
using GdeOni.Application.Complimentary.Commands.Grant.Model;
using GdeOni.Application.Complimentary.Commands.Grant.UseCase;
using GdeOni.Application.Complimentary.Commands.Revoke.Model;
using GdeOni.Application.Complimentary.Commands.Revoke.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D22. Админский эндпоинт для выдачи и отзыва бесплатного доступа.
/// Только SuperAdmin / Admin (роль перебивает Subscription-policy,
/// см. D16.5). Доменные доп-ограничения внутри use case'ов:
/// — себе выдать нельзя;
/// — Admin не управляет SuperAdmin'ом.
/// </summary>
[ApiController]
[Route("api/admin/users/{userId:guid}/complimentary-access")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AdminComplimentaryAccessController : ApiControllerBase
{
    /// <summary>
    /// Выдать пользователю <c>userId</c> бесплатный доступ. Если
    /// <see cref="GrantComplimentaryAccessRequest.UntilUtc"/> = null —
    /// доступ бессрочный. Идемпотентно: повторный вызов с теми же
    /// параметрами не двигает GrantedAt.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Grant(
        Guid userId,
        [FromBody] GrantComplimentaryAccessRequest request,
        [FromServices] IGrantComplimentaryAccessUseCase grant,
        CancellationToken cancellationToken)
    {
        var command = new GrantComplimentaryAccessCommand(userId, request.UntilUtc, request.Note);
        var result = await grant.Execute(command, cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Отозвать бесплатный доступ у пользователя <c>userId</c>. Если
    /// доступа не было — silent no-op (204). Юзер при следующем
    /// открытии профиля увидит реальный статус подписки.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid userId,
        [FromServices] IRevokeComplimentaryAccessUseCase revoke,
        CancellationToken cancellationToken)
    {
        var result = await revoke.Execute(
            new RevokeComplimentaryAccessCommand(userId),
            cancellationToken);
        return FromUnitResult(result);
    }
}
