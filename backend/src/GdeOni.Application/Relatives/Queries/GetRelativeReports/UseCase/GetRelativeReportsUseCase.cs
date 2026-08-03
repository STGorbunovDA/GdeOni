using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Relatives.Queries.GetRelativeReports.Model;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetRelativeReports.UseCase;

/// <summary>
/// Админский список жалоб на родственников (Фаза 5). Авторизация — на уровне
/// контроллера (SuperAdmin/Admin).
/// </summary>
public sealed class GetRelativeReportsUseCase(IRelativeReportRepository reportRepository)
    : IGetRelativeReportsUseCase
{
    // Очередь модерации небольшая — отдаём разумный потолок без пагинации.
    private const int Limit = 200;

    public async Task<Result<GetRelativeReportsResponse, Error>> Execute(
        GetRelativeReportsQuery query, CancellationToken cancellationToken)
    {
        var statusFilter = query.PendingOnly ? RelativeReportStatus.Pending : (RelativeReportStatus?)null;
        var reports = await reportRepository.GetReports(statusFilter, Limit, cancellationToken);

        var items = reports
            .Select(r => new RelativeReportItemResponse(
                r.Id,
                r.ReporterUserId,
                r.ReporterUserName,
                r.ReportedUserId,
                r.ReportedUserName,
                r.ReportedIsBlocked,
                r.DeceasedId,
                r.DeceasedFullName,
                r.ConversationId,
                r.Reason,
                r.CreatedAtUtc,
                r.Status.ToString(),
                r.ResolvedAtUtc,
                r.ResolutionNote))
            .ToList();

        return Result.Success<GetRelativeReportsResponse, Error>(
            new GetRelativeReportsResponse(items));
    }
}
