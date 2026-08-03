using GdeOni.API.Authorization;
using GdeOni.API.Models.Relatives;
using GdeOni.API.RateLimiting;
using GdeOni.API.Response;
using GdeOni.Application.Relatives.Commands.DeleteMessage.Model;
using GdeOni.Application.Relatives.Commands.DeleteMessage.UseCase;
using GdeOni.Application.Relatives.Commands.EditMessage.Model;
using GdeOni.Application.Relatives.Commands.EditMessage.UseCase;
using GdeOni.Application.Relatives.Commands.SendMessage.Model;
using GdeOni.Application.Relatives.Commands.SendMessage.UseCase;
using GdeOni.Application.Relatives.Commands.StartConversation.Model;
using GdeOni.Application.Relatives.Commands.StartConversation.UseCase;
using GdeOni.Application.Relatives.Commands.MarkRelativesSeen.UseCase;
using GdeOni.Application.Relatives.Commands.ReportRelative.Model;
using GdeOni.Application.Relatives.Commands.ReportRelative.UseCase;
using GdeOni.Application.Relatives.Common;
using GdeOni.Application.Relatives.Queries.GetConversation.Model;
using GdeOni.Application.Relatives.Queries.GetConversation.UseCase;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.Model;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.UseCase;
using GdeOni.Application.Relatives.Queries.GetRelativesSummary.Model;
using GdeOni.Application.Relatives.Queries.GetRelativesSummary.UseCase;
using GdeOni.Application.Relatives.Queries.ListConversations.Model;
using GdeOni.Application.Relatives.Queries.ListConversations.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    /// <summary>
    /// Фаза 4. Сводка для попапа «События» и бейджа вкладки: новые
    /// родственники (обнаружены ночным джобом, ещё не просмотрены) +
    /// непрочитанные диалоги.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<RelativesSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(
        [FromServices] IGetRelativesSummaryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Фаза 4. Отметить всех «новых» родственников просмотренными — вызывается
    /// при заходе на вкладку «Родственники», чтобы убрать уведомления и бейдж.
    /// </summary>
    [HttpPost("seen")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkSeen(
        [FromServices] IMarkRelativesSeenUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromUnitResult(result);
    }

    // ─────────────── Внутренняя переписка (Фаза 3) ───────────────

    /// <summary>
    /// Открыть (или получить существующий) диалог с родственником по карточке.
    /// Идемпотентно: один диалог на пару в контексте карточки.
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ApiResponse<RelativeConversationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartConversation(
        [FromBody] StartConversationRequest request,
        [FromServices] IStartConversationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new StartConversationCommand(request.DeceasedId, request.OtherUserId),
            cancellationToken);
        return FromResult(result);
    }

    /// <summary>Список диалогов текущего пользователя (инбокс + непрочитанные).</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ApiResponse<ListRelativeConversationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListConversations(
        [FromServices] IListConversationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>Открыть диалог по id (отмечает прочитанными сообщения собеседника).</summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RelativeConversationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] Guid id,
        [FromServices] IGetConversationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new GetConversationQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отправить сообщение. Разрешено только когда сейчас ход пользователя
    /// (собеседник ответил на предыдущее) — иначе 409 not_your_turn.
    /// </summary>
    [HttpPost("conversations/{id:guid}/messages")]
    [EnableRateLimiting(RelativeMessagesRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<RelativeConversationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] Guid id,
        [FromBody] SendMessageRequest request,
        [FromServices] ISendMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new SendMessageCommand(id, request.Text), cancellationToken);
        return FromResult(result);
    }

    /// <summary>Изменить своё последнее сообщение (пока собеседник не ответил).</summary>
    [HttpPatch("conversations/{id:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RelativeConversationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditMessage(
        [FromRoute] Guid id,
        [FromRoute] Guid messageId,
        [FromBody] EditMessageRequest request,
        [FromServices] IEditMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new EditMessageCommand(id, messageId, request.Text), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Фаза 5. Пожаловаться на собеседника (в контексте диалога). Нарушитель —
    /// второй участник диалога; жаловаться на постороннего нельзя. Повторная
    /// активная жалоба на того же человека в том же диалоге не создаётся (дедуп).
    /// </summary>
    [HttpPost("reports")]
    [ProducesResponseType(typeof(ApiResponse<ReportRelativeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Report(
        [FromBody] ReportRelativeRequest request,
        [FromServices] IReportRelativeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new ReportRelativeCommand(request.ConversationId, request.Reason),
            cancellationToken);
        return FromResult(result);
    }

    /// <summary>Удалить своё последнее сообщение (пока собеседник не ответил).</summary>
    [HttpDelete("conversations/{id:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RelativeConversationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMessage(
        [FromRoute] Guid id,
        [FromRoute] Guid messageId,
        [FromServices] IDeleteMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new DeleteMessageCommand(id, messageId), cancellationToken);
        return FromResult(result);
    }
}
