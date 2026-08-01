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

    /// <summary>
    /// D46. Батч-выборка карточек по списку id (read-only) — для функции
    /// «Поделиться»: показать получателю строки подборки и импортировать их
    /// в отслеживание. Несуществующие id просто отсутствуют в результате
    /// (карточку могли удалить между шэром и открытием). Коллекции
    /// (media/memories) не грузятся — нужны только имя/даты/место.
    /// </summary>
    Task<List<Deceased>> GetForShare(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

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
    /// True если юзер создавал хоть одну карточку умершего ИЛИ загружал
    /// хоть один медиа-файл. Используется DeleteUserUseCase: эти связи
    /// имеют OnDelete=Restrict в БД, поэтому удаление юзера без явной
    /// проверки кинуло бы 500 на FK violation.
    /// </summary>
    Task<bool> HasContentByUser(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Переназначить весь контент (карточки умерших + медиа), созданный
    /// одним юзером, на другого. Используется при удалении юзера админом:
    /// SuperAdmin становится новым автором. Для каждой переназначенной
    /// карточки добавляется запись в deceased_edits (Kind=Reassignment)
    /// с email удалённого юзера в diff'е — чтобы история «кто реально
    /// создал» не потерялась. Медиа просто UPDATE'ятся (там нет audit log).
    /// Возвращает количество затронутых строк (deceased + media).
    /// </summary>
    Task<int> ReassignContent(
        Guid fromUserId,
        string fromUserEmail,
        Guid toUserId,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// Batched-выборка проекций (id, bucket, storage_key) для главных фото
    /// списком mediaIds. Возвращает только Approved-фото — Pending/Rejected
    /// наружу не отдаём (зеркало Deceased.GetMainPhoto()). Используется
    /// в листинговых use case'ах (GetAll/Nearby/GetById), чтобы клиенту
    /// в одном ответе пришла ссылка на главное фото без N+1 запросов
    /// за /media для каждой карточки.
    /// </summary>
    Task<Dictionary<Guid, MainMediaProjection>> GetApprovedMainMedia(
        IReadOnlyList<Guid> mediaIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Физически удаляет карточку и все связанные записи (media, memories,
    /// edits, tracking, anniversary-emails) одним каскадным DELETE на
    /// стороне БД. Прямой ExecuteDelete, а не Remove+Save намеренно: связь
    /// Deceased.MainMediaId → DeceasedMedia (SET NULL) и обратная
    /// DeceasedMedia.deceased_id → Deceased (CASCADE) образуют цикл, из-за
    /// которого EF при SaveChanges (с загруженной коллекцией Media) не может
    /// упорядочить удаления и бросает "circular dependency" → 500 при
    /// удалении карточки с главным фото. Все FK на deceased_records —
    /// ON DELETE CASCADE, поэтому БД сама подчистит детей. Собственная
    /// граница UoW (минуя Save), по аналогии с RefreshToken.RevokeAllForUser.
    /// </summary>
    Task DeleteById(Guid id, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}

/// <summary>
/// Узкая проекция главного фото для листинговых эндпоинтов.
/// Bucket + StorageKey достаточно для генерации публичного URL через
/// IFileStorage.GetPublicUrl, остальные поля DeceasedMedia не нужны.
/// </summary>
public sealed record MainMediaProjection(Guid Id, string Bucket, string StorageKey);

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