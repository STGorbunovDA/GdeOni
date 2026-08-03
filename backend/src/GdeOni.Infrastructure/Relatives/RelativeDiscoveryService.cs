using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Relatives;

/// <summary>
/// Функция «Родственники» (Фаза 4). Раз в сутки (в
/// <see cref="RelativeDiscoveryOptions.RunAtHourLocal"/> по заданному
/// часовому поясу) проходит по всем активным отслеживаниям и находит пары
/// «(владелец, умерший, родственник)», которых ещё нет в
/// <c>relative_discoveries</c>. По каждой новой заводит запись с
/// <see cref="RelativeDiscovery.IsNew"/> = true — она и превращается в
/// уведомление «у вас новый родственник» в попапе «События» при входе.
///
/// «Родственник» = другой пользователь, который активно отслеживает ту же
/// карточку со связывающей связью (не «Знакомый»/«Другое», зеркало
/// <see cref="RelativeRelationships"/>), с включённым согласием и не
/// заблокированный. Дополнительно (в отличие от пассивного списка Фазы 2)
/// требуем согласие и у самого владельца: уведомление — это push, тот кто
/// выключил «Родственников», не должен получать всплывашки.
///
/// Структурно повторяет <c>AnniversaryEmailService</c>: scoped-DbContext на
/// прогон, никаких исключений наружу из цикла, graceful-stop по
/// CancellationToken. Дедупликация — уникальный индекс, поэтому повторный
/// прогон или рестарт не задублируют уведомления.
/// </summary>
internal sealed class RelativeDiscoveryService(
    IServiceProvider serviceProvider,
    IOptions<RelativeDiscoveryOptions> options,
    ILogger<RelativeDiscoveryService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly RelativeDiscoveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Ночной поиск родственников отключён (RelativeDiscovery:Enabled = false).");
            return;
        }

        var timeZone = ResolveTimeZone();

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = timeProvider.GetUtcNow();
            var nextRunUtc = ComputeNextRunUtc(nowUtc, _options.RunAtHourLocal, timeZone);
            var jitter = _options.MaxJitterSeconds > 0
                ? TimeSpan.FromSeconds(Random.Shared.Next(_options.MaxJitterSeconds))
                : TimeSpan.Zero;

            var delay = (nextRunUtc - nowUtc) + jitter;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            logger.LogInformation(
                "Следующий поиск родственников запланирован на {NextRunUtc:o} (через {Delay}).",
                nextRunUtc,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ночной поиск родственников упал.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentPairs = await LoadCurrentPairsAsync(dbContext, cancellationToken);
        if (currentPairs.Count == 0)
        {
            logger.LogInformation("Отслеживаемых родственных связей нет — прогон пуст.");
            return;
        }

        var existing = await dbContext.Set<RelativeDiscovery>()
            .AsNoTracking()
            .Select(d => new { d.OwnerUserId, d.DeceasedId, d.RelativeUserId })
            .ToListAsync(cancellationToken);

        var existingSet = existing
            .Select(x => (x.OwnerUserId, x.DeceasedId, x.RelativeUserId))
            .ToHashSet();

        var toInsert = currentPairs
            .Where(p => !existingSet.Contains(p))
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Новых родственников не найдено.");
            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var (owner, deceased, relative) in toInsert)
        {
            dbContext.Set<RelativeDiscovery>().Add(
                RelativeDiscovery.Create(owner, deceased, relative, nowUtc));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Ночной поиск родственников: заведено {Count} новых уведомлений.",
                toInsert.Count);
        }
        catch (DbUpdateException ex)
        {
            // Уникальный индекс: часть строк успел записать параллельный
            // прогон — безопасно игнорируем, уведомления уже есть.
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(
                ex,
                "Дедуп-конфликт при записи новых родственников — вероятно параллельный прогон.");
        }
    }

    /// <summary>
    /// Все текущие пары «(владелец, умерший, родственник)»: обе стороны
    /// активно отслеживают карточку, у родственника связывающая связь, обе
    /// стороны с включённым согласием и не заблокированы.
    /// </summary>
    private static async Task<List<(Guid Owner, Guid Deceased, Guid Relative)>> LoadCurrentPairsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tracked = dbContext.Set<TrackedDeceased>().AsNoTracking();

        var pairs = await (
            from owner in tracked
            where owner.Status == TrackStatus.Active
            join relative in tracked on owner.DeceasedId equals relative.DeceasedId
            where relative.Status == TrackStatus.Active
                  && relative.RelationshipType != RelationshipType.Acquaintance
                  && relative.RelationshipType != RelationshipType.Other
            join ou in dbContext.Users.AsNoTracking()
                on EF.Property<Guid>(owner, "user_id") equals ou.Id
            where ou.AllowRelativeConnections && !ou.IsBlocked
            join ru in dbContext.Users.AsNoTracking()
                on EF.Property<Guid>(relative, "user_id") equals ru.Id
            where ru.AllowRelativeConnections && !ru.IsBlocked && ru.Id != ou.Id
            select new { OwnerId = ou.Id, owner.DeceasedId, RelativeId = ru.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        return pairs
            .Select(p => (p.OwnerId, p.DeceasedId, p.RelativeId))
            .ToList();
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning(
                ex,
                "Часовой пояс '{TimeZoneId}' не найден — используем UTC.",
                _options.TimeZoneId);
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Ближайший момент UTC, когда локальное время в <paramref name="timeZone"/>
    /// будет <paramref name="hourLocal"/>:00. Если сегодня этот час уже
    /// прошёл — берём завтрашний.
    /// </summary>
    internal static DateTimeOffset ComputeNextRunUtc(
        DateTimeOffset nowUtc,
        int hourLocal,
        TimeZoneInfo timeZone)
    {
        var hour = Math.Clamp(hourLocal, 0, 23);
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);

        var todayRunLocal = new DateTimeOffset(
            localNow.Year, localNow.Month, localNow.Day, hour, 0, 0, localNow.Offset);

        var nextLocal = todayRunLocal > nowUtc
            ? todayRunLocal
            : todayRunLocal.AddDays(1);

        return nextLocal.ToUniversalTime();
    }
}
