using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// Кортеж "Deceased + расстояние до точки поиска" для возврата из
/// GetNearby. Дистанция считается в репозитории (там доступ к БД),
/// use case оборачивает в response. NB: храним и Deceased целиком,
/// а не только нужные поля — Mapper в use case достаёт что надо.
/// </summary>
public sealed record NearbyDeceasedRow(Deceased Deceased, double DistanceMeters);

public interface IDeceasedRepository
{
    Task Add(Deceased deceased, CancellationToken cancellationToken);
    Task<Deceased?> GetById(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdReadOnly(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemories(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemoriesReadOnly(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMemoryById(Guid id, Guid memoryId, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMedia(Guid id, CancellationToken cancellationToken);
    Task<Deceased?> GetByIdWithMediaById(Guid id, Guid mediaId, CancellationToken cancellationToken);
    Task<bool> ExistsById(Guid id, CancellationToken cancellationToken);
    Task<(List<Deceased> Items, int TotalCount)> GetPaged(GetAllDeceasedQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// E21. Возвращает карточки с координатами в радиусе RadiusMeters от
    /// точки (Latitude, Longitude). Реализация: bounding box pre-filter
    /// по lat/lon в SQL (узкий результат, дёшево с индексом), затем
    /// точное расстояние через BurialLocation.DistanceTo в памяти,
    /// отсечение по радиусу, сортировка по возрастанию, пагинация.
    /// Возвращает (page, totalCount) — totalCount считается ДО skip/take.
    /// </summary>
    Task<(List<NearbyDeceasedRow> Items, int TotalCount)> GetNearby(
        GetNearbyDeceasedQuery query,
        CancellationToken cancellationToken);
    Task<(List<DeceasedMedia> Items, int TotalCount)> GetMediaPaged(
        Guid deceasedId,
        MediaKind? kind,
        ModerationStatus? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken);

    /// <summary>
    /// D24. Аудит правок карточки. Возвращает страницу edit'ов с
    /// JOIN-данными о редакторе (email/displayName) для админ-таблицы.
    /// Сортировка по EditedAtUtc desc — самые свежие сверху.
    /// </summary>
    Task<(List<DeceasedEditRow> Items, int TotalCount)> GetEditsPaged(
        Guid deceasedId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// D24/F17.9. Лента всех правок по системе для админ-вкладки.
    /// JOIN на deceased (имя умершего) + user (email/имя редактора).
    /// Опциональные фильтры: deceasedId, editorUserId, диапазон дат.
    /// </summary>
    Task<(List<DeceasedEditWithCardRow> Items, int TotalCount)> GetAllEditsPaged(
        int page,
        int pageSize,
        Guid? deceasedId,
        Guid? editorUserId,
        DateTime? editedFromUtc,
        DateTime? editedToUtc,
        CancellationToken cancellationToken);

    void Delete(Deceased deceased);
    Task Save(CancellationToken cancellationToken);
}

/// <summary>
/// D24. Audit row: DeceasedEdit + резолвленные данные о редакторе.
/// Email/displayName могут быть null если юзера удалили (SET NULL FK).
/// </summary>
public sealed record DeceasedEditRow(
    DeceasedEdit Edit,
    string? EditorEmail,
    string? EditorDisplayName);

/// <summary>
/// D24/F17.9. Тот же edit + имя умершего которого правили
/// (для глобальной ленты, где deceasedId не очевиден из контекста).
/// </summary>
public sealed record DeceasedEditWithCardRow(
    DeceasedEdit Edit,
    string DeceasedFullName,
    string? EditorEmail,
    string? EditorDisplayName);