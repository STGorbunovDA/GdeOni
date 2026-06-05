using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace GdeOni.Infrastructure.Storage;

internal sealed class MinioOrphanCleanupService(
    IServiceProvider serviceProvider,
    IOptions<MinioOptions> options,
    ILogger<MinioOrphanCleanupService> logger)
    : BackgroundService
{
    // Размер пачки объектов из MinIO, для которой делается один SQL-запрос
    // по списку storage_key. На 1000 keys запрос укладывается в стандартные
    // лимиты Postgres-параметров и держит RAM ограниченной.
    private const int BatchSize = 1000;

    private readonly MinioOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanup = _options.Cleanup;

        if (!cleanup.Enabled)
        {
            logger.LogInformation("MinIO orphan cleanup отключён (Minio:Cleanup:Enabled = false).");
            return;
        }

        // Джиттер до InitialDelayMinutes минут — не даём репликам
        // дёргать MinIO+БД в одну секунду каждый цикл (см. D11.8.4,
        // паттерн скопирован из RefreshTokensCleanupService).
        var jitter = Random.Shared.Next(cleanup.InitialDelayMinutes * 60);
        try
        {
            await Task.Delay(
                TimeSpan.FromMinutes(cleanup.InitialDelayMinutes) + TimeSpan.FromSeconds(jitter),
                stoppingToken);
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
                client, dbContext, bucket, cutoff, cancellationToken);
            totalDeleted += deleted;
            totalSkipped += skipped;
        }

        logger.LogInformation(
            "MinIO orphan cleanup завершён. Удалено: {Deleted}, пропущено young: {Skipped}.",
            totalDeleted, totalSkipped);
    }

    private async Task<(int Deleted, int Skipped)> CleanupBucketAsync(
        IMinioClient client,
        AppDbContext dbContext,
        string bucket,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var listArgs = new ListObjectsArgs()
            .WithBucket(bucket)
            .WithRecursive(true);

        var deleted = 0;
        var skipped = 0;
        var batch = new List<Item>(BatchSize);

        await foreach (var item in client.ListObjectsEnumAsync(listArgs, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.IsDir) continue;

            batch.Add(item);
            if (batch.Count < BatchSize) continue;

            var (d, s) = await ProcessBatchAsync(
                client, dbContext, bucket, batch, cutoff, cancellationToken);
            deleted += d;
            skipped += s;
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            var (d, s) = await ProcessBatchAsync(
                client, dbContext, bucket, batch, cutoff, cancellationToken);
            deleted += d;
            skipped += s;
        }

        return (deleted, skipped);
    }

    private async Task<(int Deleted, int Skipped)> ProcessBatchAsync(
        IMinioClient client,
        AppDbContext dbContext,
        string bucket,
        List<Item> batch,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var keys = batch.Select(x => x.Key).ToList();

        var knownInBatch = await dbContext.Set<DeceasedMedia>()
            .AsNoTracking()
            .Where(m => m.Bucket == bucket && keys.Contains(m.StorageKey))
            .Select(m => m.StorageKey)
            .ToListAsync(cancellationToken);

        var knownSet = new HashSet<string>(knownInBatch, StringComparer.Ordinal);

        var deleted = 0;
        var skipped = 0;

        foreach (var item in batch)
        {
            // Между удалениями проверяем shutdown — иначе SIGTERM
            // в середине batch заставляет нас доделывать всю пачку
            // ещё несколько секунд, даже если ASP.NET уже хочет
            // graceful-stop'нуть.
            cancellationToken.ThrowIfCancellationRequested();
            if (knownSet.Contains(item.Key)) continue;

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
