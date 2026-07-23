using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GdeOni.API.HealthChecks;

/// <summary>
/// D21. Health-check для PostgreSQL: пингует БД через
/// <c>AppDbContext.Database.CanConnectAsync</c>. Не делает SELECT,
/// чтобы не загружать БД; CanConnect использует pool-уровневый ping.
///
/// Если БД недоступна (DNS, контейнер упал, max-connections исчерпаны),
/// возвращаем Unhealthy — и /health отдаёт 503. Балансировщик / k8s
/// liveness-probe увидит это и не будет роутить трафик на сломанный
/// инстанс.
/// </summary>
public sealed class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    /// <summary>Пингует PostgreSQL и возвращает Healthy/Unhealthy для /health.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL reachable")
                : HealthCheckResult.Unhealthy("PostgreSQL CanConnect returned false");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL unreachable", ex);
        }
    }
}
