using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Persistence.Cleanup;

internal sealed class RefreshTokensCleanupService(
    IServiceProvider serviceProvider,
    IOptions<RefreshTokensCleanupOptions> options,
    ILogger<RefreshTokensCleanupService> logger)
    : BackgroundService
{
    private readonly RefreshTokensCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Refresh tokens cleanup отключён (RefreshTokensCleanup:Enabled = false).");
            return;
        }

        // Джиттер до InitialDelayMinutes минут — реплики, стартующие
        // одновременно, не лезут в БД в одну секунду каждый цикл
        // (см. D11.7.4). Random — instance-уникальный, на каждый
        // BackgroundService свой seed.
        var jitter = Random.Shared.Next(_options.InitialDelayMinutes * 60);
        try
        {
            await Task.Delay(
                TimeSpan.FromMinutes(_options.InitialDelayMinutes) + TimeSpan.FromSeconds(jitter),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromHours(_options.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refresh tokens cleanup упал.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var nowUtc = DateTime.UtcNow;
        var revokedCutoff = nowUtc - TimeSpan.FromDays(_options.RevokedRetentionDays);
        var expiredCutoff = nowUtc - TimeSpan.FromDays(_options.ExpiredRetentionDays);

        // Один SQL DELETE WHERE (revoked старше revokedCutoff) ИЛИ
        // (expired старше expiredCutoff). Без подгрузки строк в RAM.
        var deleted = await dbContext.RefreshTokens
            .Where(t =>
                (t.RevokedAtUtc != null && t.RevokedAtUtc < revokedCutoff)
                || t.ExpiresAtUtc < expiredCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Refresh tokens cleanup завершён. Удалено: {Deleted}. RevokedCutoff: {RevokedCutoff:o}, ExpiredCutoff: {ExpiredCutoff:o}.",
            deleted,
            revokedCutoff,
            expiredCutoff);
    }
}
