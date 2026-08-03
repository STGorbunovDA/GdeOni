namespace GdeOni.Application.Relatives.Commands.ResolveRelativeReport.Model;

/// <summary>
/// Пометить жалобу разобранной (Фаза 5). Note — необязательная пометка о
/// решении админа (например, «заблокировал» / «без нарушений»).
/// </summary>
public sealed record ResolveRelativeReportCommand(Guid ReportId, string? Note);
