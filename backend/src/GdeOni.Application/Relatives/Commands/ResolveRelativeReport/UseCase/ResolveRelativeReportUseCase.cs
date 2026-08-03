using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.ResolveRelativeReport.Model;
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

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resolveResult = report.Resolve(adminIdResult.Value, command.Note, now);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        await reportRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
