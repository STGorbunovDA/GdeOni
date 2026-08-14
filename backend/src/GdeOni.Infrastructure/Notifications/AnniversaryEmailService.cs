using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Anniversaries;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Notifications;

/// <summary>
/// D37. Раз в сутки (в <see cref="AnniversaryEmailOptions.SendAtHourLocal"/>
/// по заданному часовому поясу) находит подписки с включёнными
/// напоминаниями, у которых сегодня годовщина смерти/рождения
/// отслеживаемого умершего, и шлёт email через <see cref="IEmailSender"/>.
///
/// Дедупликация — таблица <c>sent_anniversary_emails</c>: одно письмо на
/// (пользователь, умерший, тип годовщины, дата) в год. Значит повторный
/// прогон в те же сутки или рестарт письмо не задублируют.
///
/// Структурно повторяет <c>RefreshTokensCleanupService</c> /
/// <c>MinioOrphanCleanupService</c>: scoped-DbContext на прогон, никаких
/// исключений наружу из цикла, graceful-stop по CancellationToken.
/// </summary>
internal sealed class AnniversaryEmailService(
    IServiceProvider serviceProvider,
    IOptions<AnniversaryEmailOptions> options,
    ILogger<AnniversaryEmailService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly AnniversaryEmailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Email-рассылка о годовщинах отключена (AnniversaryEmails:Enabled = false).");
            return;
        }

        var timeZone = ResolveTimeZone();

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = timeProvider.GetUtcNow();
            var nextRunUtc = ComputeNextRunUtc(nowUtc, _options.SendAtHourLocal, timeZone);
            var jitter = _options.MaxJitterSeconds > 0
                ? TimeSpan.FromSeconds(Random.Shared.Next(_options.MaxJitterSeconds))
                : TimeSpan.Zero;

            var delay = (nextRunUtc - nowUtc) + jitter;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            logger.LogInformation(
                "Следующая рассылка о годовщинах запланирована на {NextRunUtc:o} (через {Delay}).",
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
                await RunOnceAsync(timeZone, stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Рассылка о годовщинах упала.");
            }
        }
    }

    private async Task RunOnceAsync(TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        if (!emailSender.IsEnabled)
        {
            logger.LogWarning(
                "Email-канал не сконфигурирован (секция Email) — рассылка о годовщинах пропущена. " +
                "Годовщины НЕ помечены отправленными и уйдут, когда SMTP включат.");
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Push дублирует письмо: почту читают не все, а годовщина — главный
        // повод вернуться. Если ключи VAPID не заданы, здесь no-op.
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushSender>();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var todayLocal = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));

        var candidates = await LoadCandidatesAsync(dbContext, todayLocal, cancellationToken);
        if (candidates.Count == 0)
        {
            logger.LogInformation("Годовщин на {Today} нет.", todayLocal);
            return;
        }

        var due = BuildDueNotifications(candidates, todayLocal);
        if (due.Count == 0)
            return;

        // Уже разосланные сегодня — чтобы не слать повторно при рестарте /
        // втором прогоне за сутки.
        var alreadySent = await dbContext.SentAnniversaryEmails
            .AsNoTracking()
            .Where(x => x.AnniversaryDate == todayLocal)
            .Select(x => new { x.UserId, x.DeceasedId, x.Kind })
            .ToListAsync(cancellationToken);

        var sentSet = alreadySent
            .Select(x => (x.UserId, x.DeceasedId, x.Kind))
            .ToHashSet();

        var sentCount = 0;
        var failedCount = 0;

        foreach (var notification in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sentSet.Contains((notification.UserId, notification.DeceasedId, notification.Kind)))
                continue;

            var message = AnniversaryEmailContent.Build(
                notification.Email,
                notification.RecipientName,
                notification.Kind,
                notification.DeceasedFullName,
                notification.YearsSince,
                _options.AppName,
                _options.AppUrl,
                notification.DaysUntil);

            try
            {
                await emailSender.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(
                    ex,
                    "Не удалось отправить письмо о годовщине ({Kind}) для {To}, умерший {DeceasedId}.",
                    notification.Kind,
                    notification.Email,
                    notification.DeceasedId);
                continue;
            }

            // Письмо ушло — дублируем пушем. Best-effort внутри отправителя:
            // сбой push не должен помешать пометить годовщину отправленной,
            // иначе на следующем прогоне письмо уйдёт повторно.
            await pushSender.SendToUserAsync(
                notification.UserId,
                AnniversaryPushTitle(notification.Kind),
                $"{notification.DeceasedFullName} — {AnniversaryPushWhen(notification.DaysUntil)}.",
                "/events",
                cancellationToken);

            dbContext.SentAnniversaryEmails.Add(SentAnniversaryEmail.Create(
                notification.UserId,
                notification.DeceasedId,
                notification.Kind,
                todayLocal,
                nowUtc));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                sentCount++;
            }
            catch (DbUpdateException ex)
            {
                // Дедуп-строку успел записать параллельный прогон — письмо
                // ушло, конфликт уникального индекса безопасно игнорируем.
                dbContext.ChangeTracker.Clear();
                logger.LogWarning(
                    ex,
                    "Дедуп-конфликт при записи лога годовщины ({Kind}) для {To} — вероятно параллельный прогон.",
                    notification.Kind,
                    notification.Email);
            }
        }

        logger.LogInformation(
            "Рассылка о годовщинах за {Today} завершена. Отправлено: {Sent}, ошибок: {Failed}.",
            todayLocal,
            sentCount,
            failedCount);
    }

    /// <summary>
    /// F42. Тянет из БД активные подписки с включёнными напоминаниями, у
    /// которых дата рождения/смерти совпадает с одним из «целевых» дней —
    /// сегодня + все возможные упреждения (за день/за 3 дня/за неделю), с
    /// поправкой на 29 февраля. Точный набор упреждений сверяется уже в
    /// памяти (<see cref="BuildDueNotifications"/>) — SQL лишь грубо фильтрует.
    /// </summary>
    private static async Task<List<Candidate>> LoadCandidatesAsync(
        AppDbContext dbContext,
        DateOnly todayLocal,
        CancellationToken cancellationToken)
    {
        var targets = BuildTargetDayKeys(todayLocal);
        var merged = new Dictionary<(Guid, Guid), Candidate>();

        foreach (var (month, day) in targets)
        {
            var rows = await (
                from t in dbContext.Set<TrackedDeceased>()
                join u in dbContext.Users on EF.Property<Guid>(t, "user_id") equals u.Id
                join d in dbContext.DeceasedRecords on t.DeceasedId equals d.Id
                where t.Status == TrackStatus.Active
                    && !u.IsBlocked
                    && (
                        (t.DeathAnniversaryLeadDaysCsv != ""
                            && d.LifePeriod.DeathDate.Month == month
                            && d.LifePeriod.DeathDate.Day == day)
                        || (t.BirthAnniversaryLeadDaysCsv != ""
                            && d.LifePeriod.BirthDate.HasValue
                            && d.LifePeriod.BirthDate.Value.Month == month
                            && d.LifePeriod.BirthDate.Value.Day == day)
                    )
                select new Candidate(
                    u.Id,
                    u.Email,
                    u.FullName ?? u.Login,
                    d.Id,
                    d.Name.FirstName,
                    d.Name.LastName,
                    d.Name.MiddleName,
                    d.LifePeriod.BirthDate,
                    d.LifePeriod.DeathDate,
                    t.DeathAnniversaryLeadDaysCsv,
                    t.BirthAnniversaryLeadDaysCsv))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
                merged[(row.UserId, row.DeceasedId)] = row;
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// F42. Раскрывает кандидатов в конкретные уведомления: по каждому и по
    /// каждому упреждению (в день / за день / за 3 / за неделю) решает,
    /// стреляет ли сегодня напоминание о годовщине смерти и/или рождения
    /// (через <see cref="AnniversaryOccurrence"/> — она же считает число лет
    /// и обрабатывает 29 февраля). Для одной годовщины совпасть может только
    /// одно упреждение (даты упреждений различны), поэтому берём первое.
    /// </summary>
    private static List<DueNotification> BuildDueNotifications(
        IEnumerable<Candidate> candidates,
        DateOnly todayLocal)
    {
        var due = new List<DueNotification>();

        foreach (var c in candidates)
        {
            var fullName = BuildFullName(c.FirstName, c.LastName, c.MiddleName);
            if (string.IsNullOrWhiteSpace(c.Email))
                continue;

            if (TryFindDueLead(ParseLeadDays(c.DeathLeadDaysCsv), c.DeathDate, todayLocal,
                    out var deathDaysUntil, out var deathYears))
            {
                due.Add(new DueNotification(
                    c.UserId, c.Email, c.RecipientName, c.DeceasedId,
                    fullName, AnniversaryKind.Death, deathYears, deathDaysUntil));
            }

            if (c.BirthDate.HasValue
                && TryFindDueLead(ParseLeadDays(c.BirthLeadDaysCsv), c.BirthDate.Value, todayLocal,
                    out var birthDaysUntil, out var birthYears))
            {
                due.Add(new DueNotification(
                    c.UserId, c.Email, c.RecipientName, c.DeceasedId,
                    fullName, AnniversaryKind.Birth, birthYears, birthDaysUntil));
            }
        }

        return due;
    }

    /// <summary>
    /// Ищет упреждение из набора, для которого сегодня + упреждение = день
    /// годовщины <paramref name="eventDate"/>. Возвращает первое совпавшее.
    /// </summary>
    private static bool TryFindDueLead(
        IReadOnlyList<int> leadDays,
        DateOnly eventDate,
        DateOnly todayLocal,
        out int daysUntil,
        out int yearsSince)
    {
        foreach (var lead in leadDays)
        {
            if (AnniversaryOccurrence.TryGet(eventDate, todayLocal.AddDays(lead), out var years))
            {
                daysUntil = lead;
                yearsSince = years;
                return true;
            }
        }

        daysUntil = 0;
        yearsSince = 0;
        return false;
    }

    private static IReadOnlyList<int> ParseLeadDays(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<int>();

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.TryParse(p, out var v) ? v : -1)
            .Where(TrackedDeceased.AllowedLeadDays.Contains)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
    }

    /// <summary>
    /// (month, day)-пары для SQL-префильтра: сегодня и все дни, до которых
    /// может стрелять упреждение (сегодня + 1/3/7), с поправкой на 29 февраля
    /// (его годовщину в невисокосный год отмечаем 28-го).
    /// </summary>
    private static List<(int Month, int Day)> BuildTargetDayKeys(DateOnly todayLocal)
    {
        var targets = new HashSet<(int, int)>();

        foreach (var lead in TrackedDeceased.AllowedLeadDays)
        {
            var target = todayLocal.AddDays(lead);
            targets.Add((target.Month, target.Day));

            if (target is { Month: 2, Day: 28 } && !DateTime.IsLeapYear(target.Year))
                targets.Add((2, 29));
        }

        return targets.ToList();
    }

    private static string BuildFullName(string firstName, string lastName, string? middleName)
    {
        var parts = new[] { lastName, firstName, middleName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(' ', parts).Trim();
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

    /// <summary>Заголовок push-уведомления о годовщине.</summary>
    private static string AnniversaryPushTitle(AnniversaryKind kind) =>
        kind == AnniversaryKind.Death ? "Дата памяти" : "День рождения";

    /// <summary>
    /// «Сегодня» / «завтра» / «через N дн.» — напоминание приходит заранее,
    /// по настроенным пользователем lead-дням.
    /// </summary>
    private static string AnniversaryPushWhen(int daysUntil) => daysUntil switch
    {
        <= 0 => "сегодня",
        1 => "завтра",
        _ => $"через {daysUntil} дн.",
    };

    private sealed record Candidate(
        Guid UserId,
        string Email,
        string RecipientName,
        Guid DeceasedId,
        string FirstName,
        string LastName,
        string? MiddleName,
        DateOnly? BirthDate,
        DateOnly DeathDate,
        string DeathLeadDaysCsv,
        string BirthLeadDaysCsv);

    private sealed record DueNotification(
        Guid UserId,
        string Email,
        string? RecipientName,
        Guid DeceasedId,
        string DeceasedFullName,
        AnniversaryKind Kind,
        int YearsSince,
        int DaysUntil);
}
