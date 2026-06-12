using GdeOni.API.Mappers;
using GdeOni.API.Models.Support;
using GdeOni.API.Response;
using GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Model;
using GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.UseCase;
using GdeOni.Application.Support.Commands.UpdateSeverity.UseCase;
using GdeOni.Application.Support.Commands.UpdateStatus.UseCase;
using GdeOni.Application.Support.Queries.GetAll.Model;
using GdeOni.Application.Support.Queries.GetAll.UseCase;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Application.Support.Queries.GetById.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D25. Админский API обращений: листинг с фильтрами, карточка,
/// смена статуса/severity. Доступ только SuperAdmin / Admin —
/// проверяется и через [Authorize], и внутри use case'ов (defense
/// in depth).
/// </summary>
[ApiController]
[Tags("Admin")]
[Route("api/admin/support-tickets")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AdminSupportTicketsController : ApiControllerBase
{
    /// <summary>
    /// Листинг с фильтрами. Чек-боксы статусов/severity в UI шлются
    /// массивом query-параметров (?statuses=Open&amp;statuses=InProgress).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetAllSupportTicketsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllSupportTicketsRequest request,
        [FromServices] IGetAllSupportTicketsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToQuery(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Карточка тикета. Полные details (jsonb), резолюция, email юзера.
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
    /// Сменить статус. При Resolved обязателен ResolutionNote.
    /// Повторный вызов на уже Resolved → 409 AlreadyResolved.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateSupportTicketStatusRequest request,
        [FromServices] IUpdateSupportTicketStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(id), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Сменить severity. На Resolved-тикете запрещено (AlreadyResolved).
    /// </summary>
    [HttpPatch("{id:guid}/severity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSeverity(
        Guid id,
        [FromBody] UpdateSupportTicketSeverityRequest request,
        [FromServices] IUpdateSupportTicketSeverityUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(id), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D35. Сделать вложение тикета (фото) главным фото указанного
    /// умершего. MinIO server-side copy support-attachments →
    /// deceased-photos, новое media auto-approve + SetMainPhoto.
    /// Вложение в тикете остаётся (история).
    /// </summary>
    [HttpPost("{ticketId:guid}/attachments/{attachmentId:guid}/promote-to-main-photo")]
    [ProducesResponseType(typeof(ApiResponse<PromoteAttachmentToMainPhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteAttachmentToMainPhoto(
        Guid ticketId,
        Guid attachmentId,
        [FromQuery] Guid deceasedId,
        [FromServices] IPromoteAttachmentToMainPhotoUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new PromoteAttachmentToMainPhotoCommand(ticketId, attachmentId, deceasedId),
            cancellationToken);
        return FromResult(result);
    }
}
