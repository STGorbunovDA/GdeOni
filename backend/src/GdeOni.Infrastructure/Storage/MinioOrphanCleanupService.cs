using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace GdeOni.Infrastructure.Storage;

internal sealed class MinioOrphanCleanupService(
    IServiceProvider serviceProvider,
    IOptions<MinioOptions> options,
    ILogger<MinioOrphanCleanupService> logger)
    : BackgroundService
{
    private readonly MinioOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanup = _options.Cleanup;

        if (!cleanup.Enabled)
        {
            logger.LogInformation("MinIO orphan cleanup отключён (Minio:Cleanup:Enabled = false).");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(cleanup.InitialDelayMinutes), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromHours(cleanup.IntervalHours);
        var ageThreshold = TimeSpan.FromHours(cleanup.OrphanAgeHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(ageThreshold, stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "MinIO orphan cleanup упал.");
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

    private async Task RunOnceAsync(TimeSpan ageThreshold, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IMinioClient>();

        var knownKeys = await dbContext.Set<DeceasedMedia>()
            .AsNoTracking()
            .Select(m => m.StorageKey)
            .ToListAsync(cancellationToken);

        var knownKeysSet = new HashSet<string>(knownKeys, StringComparer.Ordinal);
        var cutoff = DateTime.UtcNow - ageThreshold;

        var buckets = new[]
        {
            _options.Buckets.DeceasedPhotos,
            _options.Buckets.GravePhotos,
            _options.Buckets.DeceasedDocuments,
        };

        var totalDeleted = 0;
        var totalSkipped = 0;

        foreach (var bucket in buckets)
        {
            var (deleted, skipped) = await CleanupBucketAsync(
                client, bucket, knownKeysSet, cutoff, cancellationToken);
            totalDeleted += deleted;
            totalSkipped += skipped;
        }

        logger.LogInformation(
            "MinIO orphan cleanup завершён. Удалено: {Deleted}, пропущено young: {Skipped}.",
            totalDeleted, totalSkipped);
    }

    private async Task<(int Deleted, int Skipped)> CleanupBucketAsync(
        IMinioClient client,
        string bucket,
        HashSet<string> knownKeys,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var listArgs = new ListObjectsArgs()
            .WithBucket(bucket)
            .WithRecursive(true);

        var deleted = 0;
        var skipped = 0;

        await foreach (var item in client.ListObjectsEnumAsync(listArgs, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.IsDir) continue;
            if (knownKeys.Contains(item.Key)) continue;

            var lastModified = item.LastModifiedDateTime ?? DateTime.UtcNow;
            if (lastModified > cutoff)
            {
                skipped++;
                continue;
            }

            try
            {
                await client.RemoveObjectAsync(
                    new RemoveObjectArgs().WithBucket(bucket).WithObject(item.Key),
                    cancellationToken);
                deleted++;
                logger.LogInformation(
                    "MinIO orphan cleanup: удалён {Bucket}/{Key} (LastModified {LastModified:o}).",
                    bucket, item.Key, lastModified);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "MinIO orphan cleanup: не удалось удалить {Bucket}/{Key}.",
                    bucket, item.Key);
            }
        }

        return (deleted, skipped);
    }
}
