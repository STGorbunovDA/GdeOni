using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface IDeceasedRecordsApi
{
    [Post("/api/deceased-records/at-grave")]
    Task<ApiEnvelope<AddDeceasedAtGraveResponse>> AddAtGraveAsync(
        [Body] AddDeceasedAtGraveRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D15: открыт всем авторизованным. Используется для E16 — поиск
    /// существующего умершего перед добавлением (чтобы не плодить
    /// дубликаты, юзер сначала проверяет, нет ли уже карточки).
    /// </summary>
    /// <summary>
    /// birthDate / deathDate — строки в формате yyyy-MM-dd (ISO).
    /// Передаём string а не DateOnly?, потому что Refit 8 не всегда
    /// уважает [Query(Format=...)] на DateOnly — иногда уходит локальный
    /// формат (12.05.2025), и ASP.NET binder это не распознаёт. VM
    /// форматирует вручную через DateOnly.ToString("yyyy-MM-dd",
    /// CultureInfo.InvariantCulture).
    /// </summary>
    [Get("/api/deceased-records")]
    Task<ApiEnvelope<PagedResponse<DeceasedSearchItem>>> GetAllAsync(
        [Query] string? search = null,
        [Query] string? firstName = null,
        [Query] string? lastName = null,
        [Query] string? middleName = null,
        [Query] string? country = null,
        [Query] string? city = null,
        [Query] string? birthDate = null,
        [Query] string? deathDate = null,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// E17.1: карточка умершего без tracking-инфо. Используется для
    /// preview-страницы из поиска (показать "Это он?" → решение юзера
    /// подписаться или нет). После D15 endpoint открыт авторизованным.
    /// </summary>
    [Get("/api/deceased-records/{deceasedId}")]
    Task<ApiEnvelope<DeceasedDetailsResponse>> GetByIdAsync(
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// E21. Поиск умерших в радиусе RadiusMeters от точки (lat, lon).
    /// radiusMeters в диапазоне [10, 5000] — валидатор бэка отдаст 400
    /// при выходе. Результат отсортирован по DistanceMeters возрастающе.
    /// </summary>
    [Get("/api/deceased-records/nearby")]
    Task<ApiEnvelope<PagedResponse<NearbyDeceasedItem>>> GetNearbyAsync(
        [Query] double latitude,
        [Query] double longitude,
        [Query] double radiusMeters,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// E20: перезаписать координаты места захоронения. Эндпойнт исторически
    /// назывался /burial-location/from-gps, но backend сохраняет
    /// существующие адресные поля — подходит и для "поправить координаты".
    /// 403 — не автор и не админ; 409 / 400 — невалидные диапазоны.
    /// </summary>
    [Put("/api/deceased-records/{deceasedId}/burial-location/from-gps")]
    Task<ApiEnvelope<SetBurialLocationResponse>> SetBurialLocationAsync(
        Guid deceasedId,
        [Body] SetBurialLocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D24. PATCH основных полей карточки (имя, даты, описание, биография).
    /// Доступно отслеживающим эту карточку юзерам и админам — 403 если
    /// юзер не трекает. Изменения попадают в audit log (deceased_edits).
    /// </summary>
    [Patch("/api/deceased-records/{deceasedId}/main-info")]
    Task<HttpResponseMessage> UpdateMainInfoAsync(
        Guid deceasedId,
        [Body] UpdateMainInfoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D24. PATCH метаданных (эпитафия, религия, источник, военная служба,
    /// доп. инфо). Те же права что и main-info.
    /// </summary>
    [Patch("/api/deceased-records/{deceasedId}/metadata")]
    Task<HttpResponseMessage> UpdateMetadataAsync(
        Guid deceasedId,
        [Body] UpdateMetadataRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D24. PATCH места захоронения (координаты + страна/город/кладбище/
    /// участок/номер). Latitude/Longitude == null → координаты удаляются.
    /// </summary>
    [Patch("/api/deceased-records/{deceasedId}/burial-location")]
    Task<HttpResponseMessage> UpdateBurialLocationAsync(
        Guid deceasedId,
        [Body] UpdateBurialLocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Жёсткое удаление карточки умершего. Доступно только админу.
    /// Бэк возвращает 403 если у юзера нет роли SuperAdmin/Admin.
    /// </summary>
    [Delete("/api/deceased-records/{deceasedId}")]
    Task<HttpResponseMessage> DeleteAsync(
        Guid deceasedId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D24. История правок карточки умершего. Только админ — бэк вернёт 403
    /// иначе. Полезно при разборе вандализма и спорных правок.
    /// </summary>
    [Get("/api/deceased-records/{deceasedId}/edits")]
    Task<ApiEnvelope<DeceasedEditsResponse>> GetEditsAsync(
        Guid deceasedId,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken cancellationToken = default);
}
