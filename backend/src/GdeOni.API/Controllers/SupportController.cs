using CSharpFunctionalExtensions;
using GdeOni.API.Authorization;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Support;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Support.Commands.AcceptResolution.Model;
using GdeOni.Application.Support.Commands.AcceptResolution.UseCase;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Application.Support.Commands.Create.UseCase;
using GdeOni.Application.Support.Commands.CreateWithAttachments.Model;
using GdeOni.Application.Support.Commands.CreateWithAttachments.UseCase;
using GdeOni.Application.Support.Commands.AddUserMessage.Model;
using GdeOni.Application.Support.Commands.AddUserMessage.UseCase;
using GdeOni.Application.Support.Commands.Reopen.Model;
using GdeOni.Application.Support.Commands.Reopen.UseCase;
using GdeOni.Application.Support.Queries.GetAttachmentById.Model;
using GdeOni.Application.Support.Queries.GetAttachmentById.UseCase;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Application.Support.Queries.GetById.UseCase;
using GdeOni.Application.Support.Queries.GetMine.Model;
using GdeOni.Application.Support.Queries.GetMine.UseCase;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D25. Юзерский API для обращений в службу поддержки. GET /mine —
/// лента обращений с ответом админа.
///
/// <para>D44. Политика — <c>BasicAuthenticated</c>, БЕЗ проверки
/// подписки (раньше был голый <c>[Authorize]</c>, то есть DefaultPolicy
/// = RequireActiveSubscription). Обращения обязаны работать именно
/// тогда, когда доступ закрыт: у юзера кончился триал, его увёл paywall,
/// и написать в поддержку — единственный оставшийся выход. Со старой
/// политикой получался замкнутый круг: POST обращения → 403
/// <c>subscription.required</c> → клиент редиректит на paywall → юзер
/// снова жмёт «написать» → 403. Плюс это ломало сценарий оплаты
/// переводом (D44), где обращение и есть способ оплатить.</para>
/// </summary>
[ApiController]
[Tags("Support")]
[Route("api/support-tickets")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
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
    /// D33. Создать обращение с вложениями (1..5 файлов: JPEG/PNG до 10MB
    /// или PDF до 25MB, суммарно ≤50MB). Mobile дёргает эту ручку только
    /// когда юзер реально приложил файлы — без вложений идёт обычный
    /// POST /api/support-tickets (JSON, без multipart-overhead'а).
    /// </summary>
    [HttpPost("with-attachments")]
    // Лимит запроса = 50 MB суммарно (см. SupportTicket.MaxAttachmentsTotalSizeBytes).
    [RequestSizeLimit(SupportTicket.MaxAttachmentsTotalSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CreateSupportTicketWithAttachmentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateWithAttachments(
        [FromForm] SupportTicketKind kind,
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] IFormFileCollection files,
        [FromServices] ICreateSupportTicketWithAttachmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return FromResult(
                Result.Failure<CreateSupportTicketWithAttachmentsResponse, Error>(
                    Error.Validation(
                        "support_ticket.attachments.empty",
                        "Use the non-multipart endpoint when there are no attachments.")));
        }

        // Открываем потоки и докидываем их в команду — Stream'ы живут
        // до конца запроса (Kestrel держит form-data в FileBufferingReadStream).
        var attachments = new List<AttachmentUploadItem>(files.Count);
        foreach (var file in files)
        {
            attachments.Add(new AttachmentUploadItem
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Content = file.OpenReadStream(),
            });
        }

        var command = new CreateSupportTicketWithAttachmentsCommand
        {
            Kind = kind,
            Title = title,
            Description = description,
            Attachments = attachments,
        };

        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// D33. Получить вложение тикета — presigned URL для скачивания
    /// (TTL 1 час). Юзеру выдаётся только своё; админу — любое.
    /// Если тикет/вложение не найдено или нет доступа — 404 (не
    /// подсвечиваем существование чужих файлов).
    /// </summary>
    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetSupportAttachmentByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttachment(
        Guid ticketId,
        Guid attachmentId,
        [FromServices] IGetSupportAttachmentByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new GetSupportAttachmentByIdQuery(ticketId, attachmentId),
            cancellationToken);
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

    /// <summary>
    /// D44. Написать сообщение в своё обращение.
    /// </summary>
    /// <remarks>
    /// Работает, пока обращение в статусе «Открыто» или «В работе».
    /// На «Решено» у юзера есть отдельные действия — принять резолюцию
    /// или переоткрыть; на «Закрыто» переписка окончена (409).
    /// </remarks>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMessage(
        Guid id,
        [FromBody] AddSupportTicketMessageRequest request,
        [FromServices] IAddUserMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new AddUserMessageCommand(id, request.Text), cancellationToken);
        return FromUnitResult(result);
    }
}
