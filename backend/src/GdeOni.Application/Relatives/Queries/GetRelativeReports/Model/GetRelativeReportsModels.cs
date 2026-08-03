namespace GdeOni.Application.Relatives.Queries.GetRelativeReports.Model;

/// <summary>
/// Фильтр админского списка жалоб. <c>PendingOnly=true</c> — только
/// неразобранные (дефолт для очереди модерации).
/// </summary>
public sealed record GetRelativeReportsQuery(bool PendingOnly);

/// <summary>Одна жалоба в админском списке.</summary>
public sealed record RelativeReportItemResponse(
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
    string Status,
    DateTime? ResolvedAtUtc,
    string? ResolutionNote);

public sealed record GetRelativeReportsResponse(IReadOnlyList<RelativeReportItemResponse> Items);
