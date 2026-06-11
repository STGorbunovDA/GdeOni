using GdeOni.API.Mappers;
using GdeOni.API.Models.Support;
using GdeOni.API.Response;
using GdeOni.Application.Support.Commands.AcceptResolution.Model;
using GdeOni.Application.Support.Commands.AcceptResolution.UseCase;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Application.Support.Commands.Create.UseCase;
using GdeOni.Application.Support.Commands.Reopen.Model;
using GdeOni.Application.Support.Commands.Reopen.UseCase;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Application.Support.Queries.GetById.UseCase;
using GdeOni.Application.Support.Queries.GetMine.Model;
using GdeOni.Application.Support.Queries.GetMine.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D25. Юзерский API для обращений в службу поддержки. Авторизация —
/// любой authenticated юзер (admin тоже может слать обращения от
/// своего имени). GET /mine — лента обращений с ответом админа.
/// </summary>
[ApiController]
[Tags("Support")]
[Route("api/support-tickets")]
[Authorize]
public sealed class SupportController : ApiControllerBase
{
    /// <summary>
    /// Создать обращение. Severity всегда Normal — апгрейдить может
    /// только админ. Возвращает id созданного тикета.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateSupportTicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSupportTicketRequest request,
        [FromServices] ICreateSupportTicketUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Мои обращения. Отсортированы по CreatedAtUtc DESC, пагинация.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<GetMySupportTicketsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IGetMySupportTicketsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetMySupportTicketsQuery(
            Page: page == 0 ? 1 : page,
            PageSize: pageSize == 0 ? 20 : pageSize);
        var result = await useCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Карточка моего обращения. Юзеру отдаётся только свой тикет
    /// (Forbidden иначе) — админ использует /api/admin/support-tickets.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetSupportTicketByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IGetSupportTicketByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new GetSupportTicketByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// POST /api/support-tickets/{id}/accept — закрепить резолюцию
    /// ("Закрепить решено"). Только автор тикета. Только если тикет
    /// уже Resolved. Повторный Accept → 409 AlreadyAccepted.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromServices] IAcceptSupportTicketResolutionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new AcceptSupportTicketResolutionCommand(id), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// POST /api/support-tickets/{id}/reopen — переоткрыть тикет
    /// ("Продолжить спор"). Только автор. Только если тикет Resolved
    /// и юзер не закрепил резолюцию. ResolutionNote остаётся в истории,
    /// Status переходит в Open, ReopenedCount++, LastUserReply
    /// сохраняется.
    /// </summary>
    [HttpPost("{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(
        Guid id,
        [FromBody] ReopenSupportTicketRequest request,
        [FromServices] IReopenSupportTicketUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new ReopenSupportTicketCommand(id, request.UserReply), cancellationToken);
        return FromUnitResult(result);
    }
}
