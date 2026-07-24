using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class DeceasedRepository(AppDbContext dbContext, TimeProvider timeProvider) : IDeceasedRepository
{
    public async Task Add(Deceased deceased, CancellationToken cancellationToken)
    {
        await dbContext.DeceasedRecords.AddAsync(deceased, cancellationToken);
    }

    public async Task<Deceased?> GetById(Guid id, CancellationToken cancellationToken)
    {
        // Tracked-вариант для use case-ов, которые мутируют сущность
        // (Verify, Update, UploadMedia, AddMemory, ClearMetadata и т.п.).
        // Для read-only сценариев — GetByIdReadOnly (D7.58).
        return await dbContext.DeceasedRecords
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdReadOnly(Guid id, CancellationToken cancellationToken)
    {
        // AsNoTracking: query-use-case'ы (GetAgeAtDeath, GetMediaList,
        // GetDistance) читают сущность и не вызывают Save. Снапшот
        // в change-tracker'е не нужен — экономим RAM и страхуемся от
        // случайной мутации, которая бы прилетела в SaveChanges. См. D7.58.
        return await dbContext.DeceasedRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdWithMemories(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.DeceasedRecords
            .Include(x => x.Memories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdWithMemoriesReadOnly(Guid id, CancellationToken cancellationToken)
    {
        // AsNoTracking-вариант для query-use-case'ов (GetDeceasedById,
        // GetMyTrackedDeceasedDetails) — карточка + memories грузятся,
        // никто не вызывает Save. Аналог D7.67 для include'а memories.
        // См. D8.10.
        return await dbContext.DeceasedRecords
            .AsNoTracking()
            .Include(x => x.Memories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdWithMemoryById(
        Guid id,
        Guid memoryId,
        CancellationToken cancellationToken)
    {
        // Filtered Include (EF Core 5+) — грузим Deceased + ОДНУ memory.
        // Domain-методы (EditMemory/ApproveMemory/RejectMemory/RemoveMemory)
        // используют `_memories.FirstOrDefault(x => x.Id == memoryId)`, что
        // корректно отрабатывает на коллекции из 0 или 1 элемента: если
        // memory не загрузился — null → NotFound; если загрузился — он
        // единственный, изменения трекаются EF и попадают в SaveChanges
        // одиночным UPDATE/DELETE без затрагивания остальных Memories
        // карточки. См. D7.46.
        return await dbContext.DeceasedRecords
            .Include(x => x.Memories.Where(m => m.Id == memoryId))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdWithMedia(Guid id, CancellationToken cancellationToken)
    {
        // Tracked by design (D11.5.5): метод используется только из
        // мутирующих use case'ов — SetMainMediaPhoto перебирает всю
        // коллекцию Media и пересбрасывает IsMainPhoto, DeleteDeceased
        // полагается на загруженную коллекцию для каскадного удаления
        // вложенных записей. Read-only сценариев на полный набор Media
        // нет; если появятся — добавь GetByIdWithMediaReadOnly с
        // AsNoTracking по аналогии с GetByIdWithMemoriesReadOnly.
        return await dbContext.DeceasedRecords
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Deceased?> GetByIdWithMediaById(
        Guid id,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        // Filtered Include — Deceased + одна media. Доменные методы
        // (RemoveMedia/ApproveMedia/RejectMedia/UpdateMediaDescription)
        // работают через `_media.FirstOrDefault(x => x.Id == mediaId)`
        // и корректно ходят по коллекции из 0/1 элемента. RejectMedia
        // обнуляет MainMediaId — это поле самого Deceased, остальные
        // media в этой выборке не нужны. Аналог D7.46 для media. См. D7.47.
        //
        // SetMainPhoto использует GetByIdWithMedia (полная коллекция):
        // он итерирует по всем DeceasedPhoto и сбрасывает IsMainPhoto.
        return await dbContext.DeceasedRecords
            .Include(x => x.Media.Where(m => m.Id == mediaId))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsById(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.DeceasedRecords
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Dictionary<Guid, MainMediaProjection>> GetApprovedMainMedia(
        IReadOnlyList<Guid> mediaIds,
        CancellationToken cancellationToken)
    {
        if (mediaIds.Count == 0)
            return new Dictionary<Guid, MainMediaProjection>();

        // Guard: текущие callsite'ы передают max query.PageSize штук
        // (≤100 в GetAll/Nearby/MyTracked + 1 в GetById/Details). Если
        // кто-то новый передаст сюда полный список (без Take), Npgsql
        // переведёт Contains в ANY(@arr) и формально limit 65535 параметров
        // не упрётся — но запрос всё равно становится медленным
        // (full scan) и подозрительным. 1000 — мягкая граница. Если
        // понадобится больше — добавь chunking.
        const int MaxIdsPerCall = 1000;
        if (mediaIds.Count > MaxIdsPerCall)
        {
            throw new ArgumentException(
                $"GetApprovedMainMedia: mediaIds.Count={mediaIds.Count} > {MaxIdsPerCall}. " +
                "Используй chunking, если действительно нужно больше — листинговые " +
                "use case'ы ограничены PageSize.",
                nameof(mediaIds));
        }

        // Узкая проекция (id, bucket, storage_key) для Approved-фото.
        // ToDictionary — id уникальный (PK), коллизий быть не может.
        var rows = await dbContext.Set<DeceasedMedia>()
            .AsNoTracking()
            .Where(m => mediaIds.Contains(m.Id)
                        && m.ModerationStatus == ModerationStatus.Approved)
            .Select(m => new MainMediaProjection(m.Id, m.Bucket, m.StorageKey))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id);
    }

    public async Task<(List<DeceasedMedia> Items, int TotalCount)> GetMediaPaged(
        Guid deceasedId,
        MediaKind? kind,
        ModerationStatus? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<DeceasedMedia>()
            .AsNoTracking()
            .Where(x => x.DeceasedId == deceasedId);

        if (kind.HasValue)
            query = query.Where(x => x.Kind == kind.Value);

        if (moderationStatus.HasValue)
            query = query.Where(x => x.ModerationStatus == moderationStatus.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Deceased> Items, int TotalCount)> GetPaged(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.DeceasedRecords
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            dbQuery = dbQuery.Where(x =>
                EF.Functions.ILike(x.Name.FirstName, $"%{search}%") ||
                EF.Functions.ILike(x.Name.LastName, $"%{search}%") ||
                (x.Name.MiddleName != null && EF.Functions.ILike(x.Name.MiddleName, $"%{search}%")));
        }

        // E17.5: отдельные поля для имени/фамилии/отчества. Каждое
        // — ILike substring. Между собой AND'ятся, т.е. передал
        // firstName=Дмитрий + lastName=Клусевич → ищет именно такого
        // человека, а не "хоть какого-то Дмитрия или хоть какого-то Клусевича".
        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            var firstName = $"%{query.FirstName.Trim()}%";
            dbQuery = dbQuery.Where(x => EF.Functions.ILike(x.Name.FirstName, firstName));
        }

        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            var lastName = $"%{query.LastName.Trim()}%";
            dbQuery = dbQuery.Where(x => EF.Functions.ILike(x.Name.LastName, lastName));
        }

        if (!string.IsNullOrWhiteSpace(query.MiddleName))
        {
            var middleName = $"%{query.MiddleName.Trim()}%";
            dbQuery = dbQuery.Where(x =>
                x.Name.MiddleName != null &&
                EF.Functions.ILike(x.Name.MiddleName, middleName));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            // Substring-семантика, как у Search ниже (D11.9.1): иначе
            // `?country=Russia` не находил запись со страной
            // "Russian Federation".
            var country = $"%{query.Country.Trim()}%";
            dbQuery = dbQuery.Where(x =>
                x.BurialLocation != null &&
                x.BurialLocation.Country != null &&
                EF.Functions.ILike(x.BurialLocation.Country, country));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = $"%{query.City.Trim()}%";
            dbQuery = dbQuery.Where(x =>
                x.BurialLocation != null &&
                x.BurialLocation.City != null &&
                EF.Functions.ILike(x.BurialLocation.City, city));
        }

        if (query.IsVerified.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.IsVerified == query.IsVerified.Value);
        }

        if (query.CreatedFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc <= query.CreatedTo.Value);
        }

        // Фильтры по датам жизни — точное совпадение. BirthDate в
        // LifePeriod nullable, поэтому если в фильтре есть значение,
        // мы исключаем карточки с null BirthDate. DeathDate всегда есть.
        if (query.BirthDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.LifePeriod.BirthDate == query.BirthDate.Value);
        }

        if (query.DeathDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.LifePeriod.DeathDate == query.DeathDate.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<NearbyDeceasedRow> Items, int TotalCount)> GetNearby(
        GetNearbyDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        // Подход: bounding box → точное haversine в памяти → радиус-фильтр.
        //
        // Bounding box в градусах для радиуса R метров вокруг (lat, lon):
        //   latDelta = R / 111320                       (1° lat = ~111.32 км)
        //   lonDelta = R / (111320 * cos(lat))          (1° lon зависит от широты)
        //
        // Это сужает SQL-результат до маленькой коробки → планировщик может
        // использовать индекс на (latitude, longitude). Точное расстояние
        // (с учётом сферы) дальше считаем в памяти через BurialLocation.
        // DistanceTo — на отфильтрованных bbox-выборкой записях это
        // дёшево даже без spatial-индекса.
        //
        // Граничные кейсы:
        // - lat близко к полюсам (±90°): cos(lat) → 0, lonDelta → бесконечность.
        //   Защита: clamp lonDelta до 180° — при таком радиусе нам по сути
        //   нужна вся "линия широты", и bbox всё равно даст узкий результат.
        // - lon ±180° (антимеридиан): bbox может "переходить" через -180/+180.
        //   В простой версии (текущей) мы это игнорируем — карточек на
        //   антимеридиане в обозримом будущем не будет. При необходимости
        //   добавить OR-условие через раздел диапазона.
        const double MetersPerDegreeLat = 111320.0;
        var latRadians = query.Latitude * Math.PI / 180.0;
        var cosLat = Math.Cos(latRadians);

        var latDelta = query.RadiusMeters / MetersPerDegreeLat;
        // Подстраховка от cos~0 у полюсов: минимум cosLat 0.001 → lonDelta
        // около 90° максимум для радиуса 100м, что нас полностью устраивает.
        var safeCosLat = Math.Max(Math.Abs(cosLat), 0.001);
        var lonDelta = query.RadiusMeters / (MetersPerDegreeLat * safeCosLat);

        var minLat = query.Latitude - latDelta;
        var maxLat = query.Latitude + latDelta;
        var minLon = query.Longitude - lonDelta;
        var maxLon = query.Longitude + lonDelta;

        // BurialLocation owned-тип, EF умеет фильтровать по его полям.
        // Карточки без BurialLocation отсекаются автоматически — Latitude
        // в owned == null означает, что owner-сторона тоже null.
        var bboxItems = await dbContext.DeceasedRecords
            .AsNoTracking()
            .Where(x => x.BurialLocation != null &&
                        x.BurialLocation.Latitude >= minLat &&
                        x.BurialLocation.Latitude <= maxLat &&
                        x.BurialLocation.Longitude >= minLon &&
                        x.BurialLocation.Longitude <= maxLon)
            .ToListAsync(cancellationToken);

        var ranked = bboxItems
            .Select(x => new
            {
                Deceased = x,
                DistanceKm = x.BurialLocation!.DistanceTo(query.Latitude, query.Longitude),
            })
            .Where(x => x.DistanceKm * 1000.0 <= query.RadiusMeters)
            .OrderBy(x => x.DistanceKm)
            .ToList();

        var totalCount = ranked.Count;

        var page = ranked
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new NearbyDeceasedRow(x.Deceased, x.DistanceKm * 1000.0))
            .ToList();

        return (page, totalCount);
    }

    public Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken)
    {
        return dbContext.DeceasedRecords
            .AsNoTracking()
            .AnyAsync(x => x.SearchKey == searchKey, cancellationToken);
    }

    public async Task<bool> HasContentByUser(Guid userId, CancellationToken cancellationToken)
    {
        // Две короткие проверки EXISTS. Индекс ix_deceased_created_by_user_id
        // покрывает первую, индекс ix_deceased_media_uploaded_by — вторую.
        var hasDeceased = await dbContext.DeceasedRecords
            .AsNoTracking()
            .AnyAsync(x => x.CreatedByUserId == userId, cancellationToken);
        if (hasDeceased) return true;

        return await dbContext.Set<DeceasedMedia>()
            .AsNoTracking()
            .AnyAsync(x => x.UploadedByUserId == userId, cancellationToken);
    }

    public async Task<int> ReassignContent(
        Guid fromUserId,
        string fromUserEmail,
        Guid toUserId,
        CancellationToken cancellationToken)
    {
        // 1) Сначала собираем id'шники всех карточек этого автора (нужны
        // для batch-insert аудит-записей до UPDATE).
        var deceasedIds = await dbContext.DeceasedRecords
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == fromUserId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // 2) UPDATE deceased SET created_by_user_id = toUserId
        //    WHERE created_by_user_id = fromUserId.
        //    ExecuteUpdateAsync — один SQL, не подгружает агрегаты в память.
        var deceasedCount = await dbContext.DeceasedRecords
            .Where(x => x.CreatedByUserId == fromUserId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.CreatedByUserId, toUserId),
                cancellationToken);

        // 3) Аналогично media.
        var mediaCount = await dbContext.Set<DeceasedMedia>()
            .Where(x => x.UploadedByUserId == fromUserId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UploadedByUserId, toUserId),
                cancellationToken);

        // 4) Для каждой переназначенной карточки — audit-запись в deceased_edits.
        //    Сохраняем email удалённого юзера в diff'е, чтобы было видно
        //    "кто реально создал" даже спустя годы.
        //    AddRange + SaveChanges заменили цикл из N отдельных
        //    ExecuteSqlInterpolatedAsync: на юзере с 500 карточками
        //    это было 500 round-trip к БД внутри одной транзакции.
        //    EF Core 8 теперь сгруппирует их в один batched INSERT.
        if (deceasedIds.Count > 0)
        {
            var changesPayload = $"{{\"PreviousAuthor\":{{\"Old\":\"{fromUserEmail}\",\"New\":null}}}}";
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var editsToAdd = deceasedIds
                .Select(deceasedId => DeceasedEdit.CreateReassignment(
                    deceasedId, changesPayload, nowUtc))
                .ToList();

            await dbContext.Set<DeceasedEdit>().AddRangeAsync(editsToAdd, cancellationToken);
        }

        // 5) Переуступка трекингов. Уникальный индекс
        //    ux_tracked_deceased_user_id_deceased_id блокирует UPDATE если
        //    SuperAdmin уже отслеживает ту же карточку — фильтруем такие
        //    через NOT EXISTS, они уйдут Cascade при Delete(user).
        //    Также сбрасываем relationship_type на Other (99) и
        //    status на Active — SuperAdmin принимает "опеку" в нейтральном
        //    состоянии, своих заметок и тогглов удалённого юзера у него
        //    быть не должно (но PersonalNotes очищать не будем — там может
        //    быть важная информация о покойном).
        var trackedCount = await dbContext.Set<TrackedDeceased>()
            .Where(t => EF.Property<Guid>(t, "user_id") == fromUserId
                && !dbContext.Set<TrackedDeceased>()
                    .Any(t2 => EF.Property<Guid>(t2, "user_id") == toUserId
                        && t2.DeceasedId == t.DeceasedId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => EF.Property<Guid>(t, "user_id"), toUserId)
                    .SetProperty(t => t.RelationshipType, RelationshipType.Other)
                    .SetProperty(t => t.Status, TrackStatus.Active),
                cancellationToken);

        return deceasedCount + mediaCount + trackedCount;
    }

    public async Task<(List<DeceasedEditRow> Items, int TotalCount)> GetEditsPaged(
        Guid deceasedId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // LEFT JOIN на users: editor мог быть soft-deleted (FK SET NULL),
        // но запись edit'а должна остаться видимой админу как "Удалённый
        // пользователь".
        var query = from edit in dbContext.Set<DeceasedEdit>().AsNoTracking()
                    where edit.DeceasedId == deceasedId
                    join user in dbContext.Set<GdeOni.Domain.Aggregates.User.User>().AsNoTracking()
                        on edit.EditedByUserId equals user.Id into editorGrp
                    from editor in editorGrp.DefaultIfEmpty()
                    orderby edit.EditedAtUtc descending
                    select new DeceasedEditRow(
                        edit,
                        editor != null ? editor.Email : null,
                        editor != null ? (editor.FullName ?? editor.UserName) : null);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<DeceasedEditWithCardRow> Items, int TotalCount)> GetAllEditsPaged(
        int page,
        int pageSize,
        Guid? deceasedId,
        Guid? editorUserId,
        DateTime? editedFromUtc,
        DateTime? editedToUtc,
        CancellationToken cancellationToken)
    {
        // Применяем фильтры на edits ДО JOIN'ов — индексы по deceased_id
        // и edited_by_user_id уже есть (см. DeceasedEditConfiguration).
        var editsQuery = dbContext.Set<DeceasedEdit>().AsNoTracking();

        if (deceasedId.HasValue)
            editsQuery = editsQuery.Where(e => e.DeceasedId == deceasedId.Value);

        if (editorUserId.HasValue)
            editsQuery = editsQuery.Where(e => e.EditedByUserId == editorUserId.Value);

        if (editedFromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(editedFromUtc.Value.Date, DateTimeKind.Utc);
            editsQuery = editsQuery.Where(e => e.EditedAtUtc >= from);
        }

        if (editedToUtc.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(editedToUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            editsQuery = editsQuery.Where(e => e.EditedAtUtc < toExclusive);
        }

        // LEFT JOIN на users + INNER на deceased (карточка не может быть
        // null — edit живёт внутри агрегата с CASCADE). Имя берём напрямую
        // из PersonName через owned-проперти.
        var query = from edit in editsQuery
                    join d in dbContext.DeceasedRecords.AsNoTracking() on edit.DeceasedId equals d.Id
                    join user in dbContext.Set<GdeOni.Domain.Aggregates.User.User>().AsNoTracking()
                        on edit.EditedByUserId equals user.Id into editorGrp
                    from editor in editorGrp.DefaultIfEmpty()
                    orderby edit.EditedAtUtc descending
                    select new DeceasedEditWithCardRow(
                        edit,
                        d.Name.LastName + " " + d.Name.FirstName,
                        editor != null ? editor.Email : null,
                        editor != null ? (editor.FullName ?? editor.UserName) : null);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task DeleteById(Guid id, CancellationToken cancellationToken)
    {
        // Прямой каскадный DELETE в БД вместо Remove+SaveChanges: обходит
        // цикл MainMediaId ↔ deceased_id в EF-графе (иначе circular
        // dependency при удалении карточки с главным фото). Все FK на
        // deceased_records — ON DELETE CASCADE, дети (media/memories/edits/
        // tracking/anniversary-emails) уходят автоматически. См.
        // IDeceasedRepository.DeleteById.
        return dbContext.DeceasedRecords
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == "23505")
        {
            throw new UniqueConstraintException(postgresException.ConstraintName);
        }
    }
}