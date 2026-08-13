using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    private IQueryable<User> UsersQuery() => dbContext.Users;

    // F40. Публичный счётчик пользователей для стартовой страницы.
    public Task<int> CountAllAsync(CancellationToken cancellationToken)
        => dbContext.Users.CountAsync(cancellationToken);

    // Id пользователей с нужными ролями (незаблокированные) — для адресной
    // рассылки уведомлений. Массив для трансляции в SQL IN (...).
    public Task<List<Guid>> GetIdsByRoles(
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken)
    {
        var roleArray = roles as UserRole[] ?? roles.ToArray();
        return dbContext.Users.AsNoTracking()
            .Where(u => roleArray.Contains(u.Role) && !u.IsBlocked)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    // Массовая выдача комплиментарного доступа всем: одним UPDATE, только
    // продлеваем (не укорачиваем уже выданный более поздний комплимент).
    public Task<int> GrantComplimentaryAccessToAll(
        DateTime untilUtc,
        Guid grantedByAdminId,
        string? note,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Where(u => u.ComplimentaryAccessUntilUtc == null
                        || u.ComplimentaryAccessUntilUtc < untilUtc)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(u => u.ComplimentaryAccessGrantedAtUtc, nowUtc)
                    .SetProperty(u => u.ComplimentaryAccessUntilUtc, untilUtc)
                    .SetProperty(u => u.ComplimentaryAccessGrantedByAdminId, grantedByAdminId)
                    .SetProperty(u => u.ComplimentaryAccessNote, note),
                cancellationToken);
    }

    public Task<User?> GetById(Guid userId, CancellationToken cancellationToken)
    {
        // Tracked-вариант для use case-ов, мутирующих User
        // (ChangeEmail / ChangePassword / ChangeRole / UpdateProfile /
        // Delete / AddDeceasedAtGrave). Для read-only сценариев —
        // GetByIdReadOnly (D7.67).
        return UsersQuery()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdReadOnly(Guid userId, CancellationToken cancellationToken)
    {
        // AsNoTracking: query-use-case'ы (RefreshUseCase, GetCurrentUserUseCase)
        // читают User и не вызывают Save — change-tracker не нужен.
        // Аналог DeceasedRepository.GetByIdReadOnly (D7.58). См. D7.67.
        return UsersQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdWithTrackingByDeceasedId(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        // Filtered Include — User + одна tracking (по DeceasedId).
        // Доменные методы (TrackDeceased / UpdateTracking / RemoveTracking)
        // ищут tracking через `_trackedDeceasedItems.FirstOrDefault(x =>
        // x.DeceasedId == deceasedId)` и корректно работают на коллекции
        // из 0/1 элемента: если найдена — мутируется, если нет — Add
        // нового. EF трекает изменения только этой одной записи. Аналог
        // D7.46 (memory) и D7.47 (media) для tracking. См. D7.55.
        return dbContext.Users
            .Include(x => x.TrackedDeceasedItems
                .Where(t => t.DeceasedId == deceasedId))
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdWithAllTracking(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Full Include для bulk-операций (RemoveAllTracking).
        return dbContext.Users
            .Include(x => x.TrackedDeceasedItems)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<(User User, int TrackingCount)?> GetByIdWithTrackingCount(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Без Include(TrackedDeceasedItems) — для read-only сценариев,
        // которым нужен только COUNT (см. GetUserByIdUseCase). EF
        // транслирует TrackedDeceasedItems.Count() в SQL COUNT-subquery.
        var row = await UsersQuery()
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { User = x, TrackingCount = x.TrackedDeceasedItems.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : (row.User, row.TrackingCount);
    }
    
    public Task<User?> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    // Вход по email ИЛИ логину: пользователи путают «псевдоним, под которым
    // регистрировался» с email. Обе колонки хранятся в lowercase, поэтому
    // сравниваем с нормализованной строкой (регистр ввода не важен).
    public Task<User?> GetByEmailOrLogin(
        string emailOrLogin,
        CancellationToken cancellationToken)
    {
        var normalized = emailOrLogin.Trim().ToLowerInvariant();

        return dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email == normalized || x.Login == normalized,
                cancellationToken);
    }

    public Task<bool> ExistsByLogin(string login, CancellationToken cancellationToken)
    {
        var normalized = login.Trim().ToLowerInvariant();

        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Login == normalized, cancellationToken);
    }

    public Task<bool> ExistsByLoginExceptUser(
        string login,
        Guid exceptUserId,
        CancellationToken cancellationToken)
    {
        var normalized = login.Trim().ToLowerInvariant();

        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.Login == normalized && x.Id != exceptUserId,
                cancellationToken);
    }

    // Записи без логина. Штатно таких нет (миграция проставила всем, колонка
    // NOT NULL), но кнопка админа — страховка на случай, если аккаунт завели
    // в обход домена.
    public async Task<List<(Guid Id, string Email)>> GetUsersWithoutLogin(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Login == "")
            .OrderBy(x => x.RegisteredAtUtc)
            .Select(x => new { x.Id, x.Email })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.Email)).ToList();
    }

    // Точечный UPDATE через ExecuteUpdate (минуя Save) — как остальные
    // bulk-операции репозитория.
    public Task<int> SetLoginById(
        Guid userId,
        string login,
        CancellationToken cancellationToken)
    {
        var normalized = login.Trim().ToLowerInvariant();

        return dbContext.Users
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Login, normalized),
                cancellationToken);
    }

    public Task<User?> GetByPasswordResetTokenHash(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(x => x.PasswordResetTokenHash == tokenHash, cancellationToken);
    }

    public Task<User?> GetByEmailConfirmationTokenHash(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(x => x.EmailConfirmationTokenHash == tokenHash, cancellationToken);
    }

    public Task<string?> GetEmailById(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIds(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string>();

        // FullName приоритетнее: это "то, что юзер указал о себе".
        // Если он пустой — fallback на UserName (он обязателен и уникален).
        var rows = await dbContext.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName, x.UserName })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Id,
            r => string.IsNullOrWhiteSpace(r.FullName) ? r.UserName : r.FullName);
    }

    public Task<User?> GetBySubscriptionPaymentId(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        // D16. Поиск по subscription.last_payment_id для
        // ProcessPaymentWebhookUseCase. Tracked — use case будет
        // вызывать ActivateSubscription, нужен change-tracker.
        // Индекс ix_users_subscription_last_payment_id обеспечивает
        // SELECT по одному значению без full-scan.
        return UsersQuery()
            .FirstOrDefaultAsync(
                x => x.Subscription.LastPaymentId == externalPaymentId,
                cancellationToken);
    }

    public Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken)
    {
        // D8.13: AsNoTracking — EXISTS-only, материализация и change-tracker
        // не нужны. Согласовано с DeceasedRepository.ExistsById.
        return UsersQuery()
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return UsersQuery()
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == normalizedEmail,
                cancellationToken);
    }

    public Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken)
    {
        var normalizedUserName = userName
            .Trim()
            .ToLowerInvariant();

        return UsersQuery()
            .AsNoTracking()
            .AnyAsync(
                x => x.UserNameNormalized == normalizedUserName,
                cancellationToken);
    }
    
    public void Delete(User user)
    {
        dbContext.Users.Remove(user);
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
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

    public async Task<(List<(TrackedDeceased Tracking, Deceased Deceased)> Items, int TotalCount)> GetMyTrackedDeceasedPaged(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var trackedQuery = dbContext.Set<TrackedDeceased>()
            .AsNoTracking()
            .Where(x => EF.Property<Guid>(x, "user_id") == userId);

        var totalCount = await trackedQuery.CountAsync(cancellationToken);

        var pairs = await trackedQuery
            .OrderByDescending(x => x.TrackedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                dbContext.DeceasedRecords.Include(d => d.MainMedia).AsNoTracking(),
                tracking => tracking.DeceasedId,
                deceased => deceased.Id,
                (tracking, deceased) => new { tracking, deceased })
            .ToListAsync(cancellationToken);

        var items = pairs
            .Select(x => (x.tracking, x.deceased))
            .ToList();

        return (items, totalCount);
    }

    public async Task<(List<(User User, int TrackingCount)> Items, int TotalCount)> GetPaged(
        GetAllUsersQuery query,
        bool includeSuperAdmins,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        // Без Include(TrackedDeceasedItems) — раньше тянули всю коллекцию
        // подписок ради одного .Count в response. Сейчас COUNT делается
        // в SQL через subquery в Select.
        var dbQuery = UsersQuery().AsNoTracking();

        if (!includeSuperAdmins)
            dbQuery = dbQuery.Where(x => x.Role != UserRole.SuperAdmin);

        if (excludeUserId.HasValue)
            dbQuery = dbQuery.Where(x => x.Id != excludeUserId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            dbQuery = dbQuery.Where(x =>
                EF.Functions.ILike(x.Email, $"%{search}%") ||
                EF.Functions.ILike(x.UserName, $"%{search}%") ||
                EF.Functions.ILike(x.Login, $"%{search}%") ||
                (x.FullName != null && EF.Functions.ILike(x.FullName, $"%{search}%"))
            );
        }

        if (query.Role.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Role == query.Role.Value);
        }

        if (query.RegisteredAtUtc.HasValue)
        {
            var date = DateTime.SpecifyKind(query.RegisteredAtUtc.Value.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            dbQuery = dbQuery.Where(x =>
                x.RegisteredAtUtc >= date &&
                x.RegisteredAtUtc < nextDate);
        }

        // Range-фильтр: From включительно, To включительно (на полный день).
        if (query.RegisteredFromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(query.RegisteredFromUtc.Value.Date, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(x => x.RegisteredAtUtc >= from);
        }

        if (query.RegisteredToUtc.HasValue)
        {
            // То = конец указанного дня (включительно), т.е. начало следующего.
            var toExclusive = DateTime.SpecifyKind(query.RegisteredToUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            dbQuery = dbQuery.Where(x => x.RegisteredAtUtc < toExclusive);
        }

        if (query.LastLoginAtUtc.HasValue)
        {
            var date = DateTime.SpecifyKind(query.LastLoginAtUtc.Value.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            dbQuery = dbQuery.Where(x =>
                x.LastLoginAtUtc.HasValue &&
                x.LastLoginAtUtc.Value >= date &&
                x.LastLoginAtUtc.Value < nextDate);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var rows = await dbQuery
            .OrderByDescending(x => x.RegisteredAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new { User = u, TrackingCount = u.TrackedDeceasedItems.Count() })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => (x.User, x.TrackingCount))
            .ToList();

        return (items, totalCount);
    }

    public Task<bool> IsActivelyTracking(Guid userId, Guid deceasedId, CancellationToken cancellationToken)
    {
        // SELECT 1 с EXISTS — никакой загрузки User или коллекции.
        // Endpoint /tracked-deceased/{id}/exists вызывается UI часто
        // (на каждой карточке/в списке), поэтому стоит остановить
        // материализацию Tracking-коллекций.
        return dbContext.Set<TrackedDeceased>()
            .AsNoTracking()
            .AnyAsync(
                t => EF.Property<Guid>(t, "user_id") == userId
                    && t.DeceasedId == deceasedId
                    && t.Status != TrackStatus.Archived,
                cancellationToken);
    }
}