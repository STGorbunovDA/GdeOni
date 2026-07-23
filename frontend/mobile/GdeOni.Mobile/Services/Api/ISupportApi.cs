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

    /// <summary>
    /// D33. POST /api/support-tickets/with-attachments — создать обращение
    /// с 1..5 вложениями (JPEG/PNG до 10MB, PDF до 25MB, суммарно ≤50MB).
    /// Дёргается только когда юзер реально выбрал файлы — без файлов
    /// идёт обычный CreateAsync (без multipart-overhead'а).
    /// </summary>
    [Multipart]
    [Post("/api/support-tickets/with-attachments")]
    Task<ApiEnvelope<CreateSupportTicketWithAttachmentsResponse>> CreateWithAttachmentsAsync(
        [AliasAs("kind")] string kind,
        [AliasAs("title")] string title,
        [AliasAs("description")] string description,
        [AliasAs("files")] IEnumerable<StreamPart> files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D33. GET /api/support-tickets/{ticketId}/attachments/{attachmentId} —
    /// presigned URL для скачивания/просмотра вложения. Юзер видит
    /// только своё, админ — любое. 404 при отсутствии доступа.
    /// </summary>
    [Get("/api/support-tickets/{ticketId}/attachments/{attachmentId}")]
    Task<ApiEnvelope<GetSupportAttachmentByIdResponse>> GetAttachmentAsync(
        Guid ticketId,
        Guid attachmentId,
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

    /// <summary>
    /// D44. POST /api/support-tickets/{id}/messages — написать в своё
    /// обращение, не меняя статус. Работает, пока обращение «Открыто»
    /// или «В работе»; на «Решено» есть отдельные действия (принять /
    /// переоткрыть), на «Закрыто» бэк отдаст 409.
    /// </summary>
    [Post("/api/support-tickets/{id}/messages")]
    Task<HttpResponseMessage> AddMessageAsync(
        Guid id,
        [Body] AddSupportTicketMessageRequest request,
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

    /// <summary>
    /// D40. POST /api/admin/support-tickets/{id}/force-close — закрыть
    /// обращение принудительно, из любого статуса.
    ///
    /// Resolved не терминален: точку в нём ставит юзер (accept-resolution),
    /// а он может просто забыть — и обращение висит в списке вечно.
    /// CloseNote обязателен, уходит юзеру в переписку.
    /// </summary>
    [Post("/api/admin/support-tickets/{id}/force-close")]
    Task<HttpResponseMessage> ForceCloseAsync(
        Guid id,
        [Body] ForceCloseSupportTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D44. POST /api/admin/support-tickets/{id}/messages — ответить
    /// в обращении, НЕ меняя статус. Раньше сообщение админа появлялось
    /// только побочным эффектом резолюции или закрытия. Статус здесь
    /// не ограничен — дописать можно на любой стадии.
    /// </summary>
    [Post("/api/admin/support-tickets/{id}/messages")]
    Task<HttpResponseMessage> AddAdminMessageAsync(
        Guid id,
        [Body] AddSupportTicketMessageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D35. POST /api/admin/support-tickets/{ticketId}/attachments/{attId}/copy-to-deceased
    /// — скопировать вложение в media умершего:
    ///   mediaKind=DeceasedPhoto + makeMain=true → главное фото;
    ///   mediaKind=DeceasedPhoto + makeMain=false → в галерею;
    ///   mediaKind=GravePhoto → фото могилы;
    ///   mediaKind=Document (для PDF) → документ умершего.
    /// Только админ. Вложение в тикете остаётся.
    /// </summary>
    [Post("/api/admin/support-tickets/{ticketId}/attachments/{attachmentId}/copy-to-deceased")]
    Task<ApiEnvelope<CopyAttachmentToDeceasedMediaResponse>> CopyAttachmentToDeceasedAsync(
        Guid ticketId,
        Guid attachmentId,
        [Query] Guid deceasedId,
        [Query] string mediaKind,
        [Query] bool makeMain,
        CancellationToken cancellationToken = default);
}
