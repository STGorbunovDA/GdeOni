using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace GdeOni.API.HealthChecks;

/// <summary>
/// D21. Health-check для MinIO: пингует object-storage через
/// <c>ListBucketsAsync</c>. Это самый дешёвый вызов, который требует
/// и сетевого коннекта, и валидных credentials (т.е. ловит и DNS-,
/// и auth-ошибки).
///
/// Используется в <c>/health</c> наряду с PostgreSQL. Без MinIO
/// нельзя загружать фото и документы (D13), поэтому если MinIO
/// недоступен — сервис фактически degraded, и k8s/балансировщик
/// должен это знать.
/// </summary>
public sealed class MinioHealthCheck(IMinioClient minioClient) : IHealthCheck
{
    /// <summary>Пингует MinIO через ListBuckets и возвращает Healthy/Unhealthy для /health.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await minioClient.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy("MinIO reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO unreachable", ex);
        }
    }
}
