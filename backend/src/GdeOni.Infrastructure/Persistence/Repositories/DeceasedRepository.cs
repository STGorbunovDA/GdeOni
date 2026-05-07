using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class DeceasedRepository(AppDbContext dbContext) : IDeceasedRepository
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

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsBySearchKey(string searchKey, CancellationToken cancellationToken)
    {
        return dbContext.DeceasedRecords
            .AsNoTracking()
            .AnyAsync(x => x.SearchKey == searchKey, cancellationToken);
    }

    public void Delete(Deceased deceased)
    {
        dbContext.DeceasedRecords.Remove(deceased);
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