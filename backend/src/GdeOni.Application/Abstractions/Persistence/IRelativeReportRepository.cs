using GdeOni.Domain.Aggregates.Relatives;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// Строка админского списка жалоб: жалоба + разрешённые в памяти имена
/// сторон, ФИО умершего и флаг блокировки нарушителя (чтобы админ видел,
/// заблокирован ли он уже).
/// </summary>
public sealed record RelativeReportListItem(
    Guid Id,
    Guid ReporterUserId,
    string ReporterUserName,
    Guid ReportedUserId,
    string ReportedUserName,
    bool ReportedIsBlocked,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid? ConversationId,
    string Reason,
    DateTime CreatedAtUtc,
    RelativeReportStatus Status,
    DateTime? ResolvedAtUtc,
    string? ResolutionNote);

public interface IRelativeReportRepository
{
    Task Add(RelativeReport report, CancellationToken cancellationToken);

    Task<RelativeReport?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Есть ли уже неразобранная жалоба от этого репортера на этого же
    /// пользователя в контексте того же диалога — для дедупа спама.
    /// </summary>
    Task<bool> HasPendingReport(
        Guid reporterUserId,
        Guid reportedUserId,
        Guid? conversationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Список жалоб для админки. <paramref name="statusFilter"/> = null —
    /// все; иначе только с этим статусом. Сортировка: сначала Pending, затем
    /// новые. <paramref name="limit"/> ограничивает выборку (модерационная
    /// очередь небольшая).
    /// </summary>
    Task<List<RelativeReportListItem>> GetReports(
        RelativeReportStatus? statusFilter,
        int limit,
        CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
