using GdeOni.Application.Admin.Queries.GetAdminStats.Model;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// F38. Read-model для админской справки.
///
/// Единственный репозиторий, который отдаёт наружу готовый Response, а не
/// доменные сущности: это чистая аналитика по нескольким агрегатам сразу
/// (users + deceased + media + support + payments). Тянуть ради счётчиков
/// сами агрегаты в память — бессмысленно; COUNT(*) должен остаться в SQL.
/// </summary>
public interface IAdminStatsRepository
{
    Task<AdminStatsResponse> GetStats(CancellationToken cancellationToken);
}
