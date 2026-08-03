using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Relatives;

/// <summary>
/// Жалоба на родственника (Фаза 5). Пользователь жалуется на собеседника в
/// контексте карточки/диалога — админ разбирает и при необходимости блокирует
/// нарушителя (существующий механизм User.Block, который автоматически убирает
/// его из всей функции «Родственники»). Сама жалоба лишь фиксирует обращение
/// и хранит решение админа.
/// </summary>
public sealed class RelativeReport : Entity<Guid>
{
    public const int MaxReasonLength = 1000;
    public const int MaxResolutionNoteLength = 1000;

    public Guid ReporterUserId { get; }
    public Guid ReportedUserId { get; }
    public Guid DeceasedId { get; }

    /// <summary>Диалог-контекст, если жалоба подана из переписки (может быть null).</summary>
    public Guid? ConversationId { get; }

    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; }

    public RelativeReportStatus Status { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }

    private RelativeReport() : base(Guid.Empty) { }

    private RelativeReport(
        Guid id,
        Guid reporterUserId,
        Guid reportedUserId,
        Guid deceasedId,
        Guid? conversationId,
        string reason,
        DateTime nowUtc)
        : base(id)
    {
        ReporterUserId = reporterUserId;
        ReportedUserId = reportedUserId;
        DeceasedId = deceasedId;
        ConversationId = conversationId;
        Reason = reason;
        CreatedAtUtc = nowUtc;
        Status = RelativeReportStatus.Pending;
    }

    public static Result<RelativeReport, Error> Create(
        Guid reporterUserId,
        Guid reportedUserId,
        Guid deceasedId,
        Guid? conversationId,
        string reason,
        DateTime nowUtc)
    {
        if (reporterUserId == reportedUserId)
            return Errors.Relatives.CannotReportSelf();

        var normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            return Errors.Relatives.ReportReasonRequired();
        if (normalized.Length > MaxReasonLength)
            return Errors.Relatives.ReportReasonTooLong(MaxReasonLength);

        return Result.Success<RelativeReport, Error>(
            new RelativeReport(
                Guid.NewGuid(),
                reporterUserId,
                reportedUserId,
                deceasedId,
                conversationId,
                normalized,
                nowUtc));
    }

    /// <summary>
    /// Пометить жалобу разобранной. Идемпотентно: повторный вызов на уже
    /// разобранной — no-op success (не перезаписываем автора/время решения).
    /// </summary>
    public UnitResult<Error> Resolve(Guid adminId, string? note, DateTime nowUtc)
    {
        if (Status == RelativeReportStatus.Resolved)
            return UnitResult.Success<Error>();

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote is not null && normalizedNote.Length > MaxResolutionNoteLength)
            return Errors.Relatives.ReportReasonTooLong(MaxResolutionNoteLength);

        Status = RelativeReportStatus.Resolved;
        ResolvedByUserId = adminId;
        ResolvedAtUtc = nowUtc;
        ResolutionNote = normalizedNote;
        return UnitResult.Success<Error>();
    }
}
