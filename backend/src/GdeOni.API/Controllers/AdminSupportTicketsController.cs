using GdeOni.API.Mappers;
using GdeOni.API.Models.Support;
using GdeOni.API.Response;
using GdeOni.Application.Support.Commands.AddAdminMessage.Model;
using GdeOni.Application.Support.Commands.AddAdminMessage.UseCase;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Model;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.UseCase;
using GdeOni.Application.Support.Commands.ForceClose.UseCase;
using GdeOni.Application.Support.Commands.UpdateSeverity.UseCase;
using GdeOni.Application.Support.Commands.UpdateStatus.UseCase;
using GdeOni.Application.Support.Queries.GetAll.Model;
using GdeOni.Application.Support.Queries.GetAll.UseCase;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Application.Support.Queries.GetById.UseCase;
using GdeOni.Domain.Shared;
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
// D44. ТОЛЬКО SuperAdmin, обычные админы сюда не допускаются.
// Переписка в обращениях идёт про оплату: там платёжные реквизиты,
// договорённости о переводах и решение о выдаче бесплатного доступа —
// это зона владельца сервиса, а не любого администратора.
[Authorize(Roles = "SuperAdmin")]
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
    /// D40. Закрыть обращение принудительно — из любого статуса, не дожидаясь
    /// подтверждения пользователя.
    ///
    /// Зачем: Resolved не терминален — точку в нём ставит юзер
    /// (accept-resolution), а он может просто забыть, и обращение висит
    /// в списке вечно. CloseNote обязателен и уходит юзеру в переписку.
    ///
    /// Повторный вызов на уже закрытом → 409 AlreadyClosed.
    /// </summary>
    [HttpPost("{id:guid}/force-close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ForceClose(
        Guid id,
        [FromBody] ForceCloseSupportTicketRequest request,
        [FromServices] IForceCloseSupportTicketUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(id), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D44. Ответить в обращении, не меняя его статус.
    /// </summary>
    /// <remarks>
    /// До D44 сообщение админа можно было создать только побочным
    /// эффектом резолюции или принудительного закрытия — то есть чтобы
    /// задать уточняющий вопрос, приходилось помечать обращение
    /// решённым. Теперь ответ и смена статуса развязаны.
    ///
    /// Статус не ограничен: дописать можно на любой стадии, включая
    /// закрытое обращение.
    /// </remarks>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(
        Guid id,
        [FromBody] AddSupportTicketMessageRequest request,
        [FromServices] IAddAdminMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new AddAdminMessageCommand(id, request.Text), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D35. Скопировать вложение тикета в media указанного умершего.
    /// Универсальная ручка с параметрами:
    ///   - mediaKind = DeceasedPhoto | GravePhoto | Document;
    ///   - makeMain = true (только для DeceasedPhoto) — сделать главным.
    /// MinIO server-side copy support-attachments → bucket для kind;
    /// новое media auto-approve, при makeMain — SetMainPhoto.
    /// Вложение в тикете остаётся.
    /// </summary>
    [HttpPost("{ticketId:guid}/attachments/{attachmentId:guid}/copy-to-deceased")]
    [ProducesResponseType(typeof(ApiResponse<CopyAttachmentToDeceasedMediaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CopyAttachmentToDeceased(
        Guid ticketId,
        Guid attachmentId,
        [FromQuery] Guid deceasedId,
        [FromQuery] MediaKind mediaKind,
        [FromQuery] bool makeMain,
        [FromServices] ICopyAttachmentToDeceasedMediaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new CopyAttachmentToDeceasedMediaCommand(
                ticketId, attachmentId, deceasedId, mediaKind, makeMain),
            cancellationToken);
        return FromResult(result);
    }
}
