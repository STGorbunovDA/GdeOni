using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.ResolveRelativeReport.Model;
using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.ResolveRelativeReport.UseCase;

/// <summary>
/// Пометить жалобу разобранной (Фаза 5). Авторизация — на уровне контроллера
/// (SuperAdmin/Admin). Блокировка нарушителя — отдельная операция (существующий
/// User.Block через /api/users/{id}/block); resolve лишь закрывает жалобу.
/// </summary>
public sealed class ResolveRelativeReportUseCase(
    IRelativeReportRepository reportRepository,
    ICurrentUserService currentUserService,
    INotificationService notificationService,
    TimeProvider timeProvider)
    : IResolveRelativeReportUseCase
{
    public async Task<UnitResult<Error>> Execute(
        ResolveRelativeReportCommand command, CancellationToken cancellationToken)
    {
        var adminIdResult = currentUserService.GetCurrentUserId();
        if (adminIdResult.IsFailure)
            return adminIdResult.Error;

        var report = await reportRepository.GetById(command.ReportId, cancellationToken);
        if (report is null)
            return Errors.Relatives.ReportNotFound();

        // До вызова: разбирали ли уже? Resolve идемпотентен (повтор — no-op),
        // поэтому уведомляем автора только при реальном переходе Pending→Resolved.
        var wasPending = report.Status == RelativeReportStatus.Pending;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resolveResult = report.Resolve(adminIdResult.Value, command.Note, now);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        await reportRepository.Save(cancellationToken);

        if (wasPending)
        {
            await notificationService.NotifyUserAsync(
                report.ReporterUserId,
                NotificationKind.RelativeReportResolved,
                "Ваша жалоба рассмотрена",
                report.ResolutionNote,
                null,
                cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}
