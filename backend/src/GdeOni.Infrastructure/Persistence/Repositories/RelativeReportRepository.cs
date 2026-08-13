using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// Функция «Родственники» (Фаза 5): жалобы на модерацию. Список для админки
/// собираем батч-запросами (имена сторон, ФИО умершего, флаг блокировки) —
/// как в RelativeConversationRepository. ФИО умершего склеиваем в памяти
/// (PersonName.FullName вычисляемое, в SQL не транслируется).
/// </summary>
public sealed class RelativeReportRepository(AppDbContext dbContext)
    : IRelativeReportRepository
{
    public async Task Add(RelativeReport report, CancellationToken cancellationToken)
    {
        await dbContext.RelativeReports.AddAsync(report, cancellationToken);
    }

    public Task<RelativeReport?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.RelativeReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<bool> HasPendingReport(
        Guid reporterUserId,
        Guid reportedUserId,
        Guid? conversationId,
        CancellationToken cancellationToken)
    {
        return dbContext.RelativeReports.AsNoTracking().AnyAsync(
            r => r.ReporterUserId == reporterUserId
                 && r.ReportedUserId == reportedUserId
                 && r.ConversationId == conversationId
                 && r.Status == RelativeReportStatus.Pending,
            cancellationToken);
    }

    public async Task<List<RelativeReportListItem>> GetReports(
        RelativeReportStatus? statusFilter,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RelativeReports.AsNoTracking();
        if (statusFilter is not null)
            query = query.Where(r => r.Status == statusFilter);

        var reports = await query
            // Сначала неразобранные (Pending=0 < Resolved=1), затем новые.
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (reports.Count == 0)
            return new List<RelativeReportListItem>();

        var userIds = reports
            .SelectMany(r => new[] { r.ReporterUserId, r.ReportedUserId })
            .Distinct()
            .ToList();
        var deceasedIds = reports.Select(r => r.DeceasedId).Distinct().ToList();

        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Login, u.IsBlocked })
            .ToListAsync(cancellationToken);
        // Стороны жалобы показываем так же, как везде: полное имя, иначе логин.
        var userNames = users.ToDictionary(
            u => u.Id,
            u => User.BuildDisplayName(u.FullName, u.Login));
        var userBlocked = users.ToDictionary(u => u.Id, u => u.IsBlocked);

        var deceasedNames = (await dbContext.DeceasedRecords.AsNoTracking()
            .Where(d => deceasedIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name.FirstName, d.Name.LastName, d.Name.MiddleName })
            .ToListAsync(cancellationToken))
            .ToDictionary(d => d.Id, d => BuildFullName(d.LastName, d.FirstName, d.MiddleName));

        return reports.Select(r => new RelativeReportListItem(
            r.Id,
            r.ReporterUserId,
            userNames.GetValueOrDefault(r.ReporterUserId, "—"),
            r.ReportedUserId,
            userNames.GetValueOrDefault(r.ReportedUserId, "—"),
            userBlocked.GetValueOrDefault(r.ReportedUserId, false),
            r.DeceasedId,
            deceasedNames.GetValueOrDefault(r.DeceasedId, "—"),
            r.ConversationId,
            r.Reason,
            r.CreatedAtUtc,
            r.Status,
            r.ResolvedAtUtc,
            r.ResolutionNote)).ToList();
    }

    public Task Save(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    // Зеркало PersonName.FullName: «Фамилия Имя Отчество» без пустых частей.
    private static string BuildFullName(string lastName, string firstName, string? middleName) =>
        string.Join(" ", new[] { lastName, firstName, middleName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
}
