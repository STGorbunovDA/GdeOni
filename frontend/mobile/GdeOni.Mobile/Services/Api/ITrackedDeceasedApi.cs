using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface ITrackedDeceasedApi
{
    [Get("/api/users/me/tracked-deceased")]
    Task<ApiEnvelope<PagedResponse<TrackedDeceasedListItem>>> GetListAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    [Get("/api/users/me/tracked-deceased/{deceasedId}")]
    Task<ApiEnvelope<MyTrackedDeceasedDetailsResponse>> GetDetailsAsync(
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    [Patch("/api/users/me/tracked-deceased/{deceasedId}")]
    Task<HttpResponseMessage> UpdateAsync(
        Guid deceasedId,
        [Body] UpdateTrackingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Подписаться на отслеживание существующего умершего (E16). Идемпотентен:
    /// если уже отслеживаешь (Active/Muted/Archived) — переводит обратно в Active.
    /// </summary>
    [Post("/api/users/me/tracked-deceased/{deceasedId}")]
    Task<ApiEnvelope<TrackDeceasedResponse>> TrackAsync(
        Guid deceasedId,
        [Body] AddMeTrackingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверка: отслеживает ли текущий пользователь этого умершего.
    /// Используется на preview-странице (E17.1), чтобы показать
    /// "Открыть мою карточку" вместо "Добавить в отслеживание".
    /// </summary>
    [Get("/api/users/me/tracked-deceased/{deceasedId}/exists")]
    Task<ApiEnvelope<IsTrackedResponse>> IsTrackedAsync(
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Физическое удаление tracking-записи. В мобильном UI пользователю
    /// не доступно (E9.1 — вместо этого PATCH со Status=Archived).
    /// Контракт оставлен для будущей админской панели.
    /// </summary>
    [Delete("/api/users/me/tracked-deceased/{deceasedId}")]
    Task<HttpResponseMessage> UntrackAsync(
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает координаты могилы + deep-link'и (yandex / google / 2gis)
    /// для построения маршрута. Доступно только активному tracker'у.
    /// 409 — у могилы нет координат. mode — строковое имя RoutingMode
    /// (Auto/Pedestrian/MassTransit/Bicycle), backend парсит case-insensitive.
    /// </summary>
    [Get("/api/users/me/tracked-deceased/{deceasedId}/route")]
    Task<ApiEnvelope<RouteResponse>> GetRouteAsync(
        Guid deceasedId,
        [Query] double fromLat,
        [Query] double fromLon,
        [Query] string mode,
        CancellationToken cancellationToken = default);
}
