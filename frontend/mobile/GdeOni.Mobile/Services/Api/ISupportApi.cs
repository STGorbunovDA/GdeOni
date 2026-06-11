using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// D25 mobile. Юзерские и админские эндпоинты обращений в службу
/// поддержки. В UI слово "обращение"; на бэке сущность support_ticket.
/// Все требуют auth; админ-эндпоинты дополнительно проверяют роль
/// (403 если не Admin/SuperAdmin).
/// </summary>
public interface ISupportApi
{
    // ───────── Юзерские ─────────

    /// <summary>POST /api/support-tickets — создать обращение от текущего юзера.</summary>
    [Post("/api/support-tickets")]
    Task<ApiEnvelope<CreateSupportTicketResponse>> CreateAsync(
        [Body] CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>GET /api/support-tickets/mine — лента моих обращений.</summary>
    [Get("/api/support-tickets/mine")]
    Task<ApiEnvelope<GetMySupportTicketsResponse>> GetMineAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET /api/support-tickets/{id} — карточка обращения. Юзер видит
    /// только своё (бэк отдаст 403), админ — любое.
    /// </summary>
    [Get("/api/support-tickets/{id}")]
    Task<ApiEnvelope<GetSupportTicketByIdResponse>> GetMineByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST /api/support-tickets/{id}/accept — закрепить резолюцию.
    /// Только автор, только Resolved. 409 если уже Accepted.
    /// </summary>
    [Post("/api/support-tickets/{id}/accept")]
    Task<HttpResponseMessage> AcceptAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST /api/support-tickets/{id}/reopen — переоткрыть тикет.
    /// Только автор, только Resolved и не Accepted.
    /// </summary>
    [Post("/api/support-tickets/{id}/reopen")]
    Task<HttpResponseMessage> ReopenAsync(
        Guid id,
        [Body] ReopenSupportTicketRequest request,
        CancellationToken cancellationToken = default);

    // ───────── Админские ─────────

    /// <summary>
    /// GET /api/admin/support-tickets — листинг с фильтрами.
    /// statuses[] и severities[] — массивы для чек-боксного UI:
    /// клиент шлёт несколько query-параметров одного имени.
    /// </summary>
    [Get("/api/admin/support-tickets")]
    Task<ApiEnvelope<GetAllSupportTicketsResponse>> GetAdminAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] Guid? userId = null,
        [Query(CollectionFormat.Multi)] string[]? statuses = null,
        [Query(CollectionFormat.Multi)] string[]? severities = null,
        [Query] string? kind = null,
        [Query] string? source = null,
        [Query(Format = "O")] DateTime? createdFromUtc = null,
        [Query(Format = "O")] DateTime? createdToUtc = null,
        [Query] string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>GET /api/admin/support-tickets/{id} — админский просмотр карточки.</summary>
    [Get("/api/admin/support-tickets/{id}")]
    Task<ApiEnvelope<GetSupportTicketByIdResponse>> GetAdminByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PATCH /api/admin/support-tickets/{id}/status — сменить статус.
    /// При Resolved обязателен ResolutionNote.
    /// </summary>
    [Patch("/api/admin/support-tickets/{id}/status")]
    Task<HttpResponseMessage> UpdateStatusAsync(
        Guid id,
        [Body] UpdateSupportTicketStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>PATCH /api/admin/support-tickets/{id}/severity — сменить severity.</summary>
    [Patch("/api/admin/support-tickets/{id}/severity")]
    Task<HttpResponseMessage> UpdateSeverityAsync(
        Guid id,
        [Body] UpdateSupportTicketSeverityRequest request,
        CancellationToken cancellationToken = default);
}
