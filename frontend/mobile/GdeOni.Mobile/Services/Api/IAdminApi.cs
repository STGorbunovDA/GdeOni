using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// F17.9 mobile. Объединяет админ-эндпоинты разных ресурсов (правки,
/// юзеры, платежи) для одного места под админ-вкладку.
/// </summary>
public interface IAdminApi
{
    /// <summary>GET /api/admin/edits — все правки карточек по системе.</summary>
    [Get("/api/admin/edits")]
    Task<ApiEnvelope<AllEditsResponse>> GetAllEditsAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET /api/users — список пользователей с пагинацией. Возвращает
    /// также счётчик отслеживаемых карточек.
    /// </summary>
    [Get("/api/users")]
    Task<ApiEnvelope<AdminUsersResponse>> GetUsersAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? search = null,
        [Query] string? role = null,
        [Query(Format = "O")] DateTime? registeredFromUtc = null,
        [Query(Format = "O")] DateTime? registeredToUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>GET /api/users/{id} — детали конкретного пользователя.</summary>
    [Get("/api/users/{userId}")]
    Task<ApiEnvelope<AdminUserDetailsDto>> GetUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>PUT /api/users/{id}/role — смена роли.</summary>
    [Put("/api/users/{userId}/role")]
    Task<HttpResponseMessage> ChangeRoleAsync(
        Guid userId,
        [Body] ChangeRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>D22. POST /api/admin/users/{id}/complimentary-access.</summary>
    [Post("/api/admin/users/{userId}/complimentary-access")]
    Task<HttpResponseMessage> GrantComplimentaryAsync(
        Guid userId,
        [Body] GrantComplimentaryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>D22. DELETE /api/admin/users/{id}/complimentary-access.</summary>
    [Delete("/api/admin/users/{userId}/complimentary-access")]
    Task<HttpResponseMessage> RevokeComplimentaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Моментально снять активную подписку у юзера. Status переходит
    /// в Expired, доступ блокируется при следующем запросе.
    /// </summary>
    [Delete("/api/admin/users/{userId}/subscription")]
    Task<HttpResponseMessage> RevokeSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Перезапустить пробный период подписки (default 30 дней из
    /// SubscriptionOptions). DurationDays опционально — если нужно
    /// нестандартный срок.
    /// </summary>
    [Post("/api/admin/users/{userId}/subscription/trial")]
    Task<HttpResponseMessage> RestartTrialAsync(
        Guid userId,
        [Body] RestartTrialRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Список отслеживаний конкретного юзера.</summary>
    [Get("/api/admin/users/{userId}/tracked-deceased")]
    Task<ApiEnvelope<AdminUserTrackedResponse>> GetUserTrackedAsync(
        Guid userId,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Снять одно отслеживание у юзера.</summary>
    [Delete("/api/admin/users/{userId}/tracked-deceased/{deceasedId}")]
    Task<HttpResponseMessage> RemoveUserTrackingAsync(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    /// <summary>Снять все отслеживания у юзера. Возвращает количество удалённых.</summary>
    [Delete("/api/admin/users/{userId}/tracked-deceased")]
    Task<ApiEnvelope<RemoveAllTrackingResponse>> RemoveAllUserTrackingAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>D23. GET /api/admin/payments — все платежи.</summary>
    [Get("/api/admin/payments")]
    Task<ApiEnvelope<AdminPaymentsResponse>> GetPaymentsAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? status = null,
        CancellationToken cancellationToken = default);
}
