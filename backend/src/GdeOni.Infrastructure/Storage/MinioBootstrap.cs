using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace GdeOni.Infrastructure.Storage;

internal static class MinioBootstrap
{
    internal static async Task EnsureBucketsAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILogger<MinioFileStorage>>();
        var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
        var client = sp.GetRequiredService<IMinioClient>();

        await EnsureBucketAsync(
            client, options.Buckets.DeceasedPhotos, publicRead: true, logger, cancellationToken);

        await EnsureBucketAsync(
            client, options.Buckets.GravePhotos, publicRead: true, logger, cancellationToken);

        await EnsureBucketAsync(
            client, options.Buckets.DeceasedDocuments, publicRead: false, logger, cancellationToken);
    }

    private static async Task EnsureBucketAsync(
        IMinioClient client,
        string bucket,
        bool publicRead,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucket);
        var exists = await client.BucketExistsAsync(existsArgs, cancellationToken);

        if (!exists)
        {
            await client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucket),
                cancellationToken);

            logger.LogInformation("MinIO: bucket '{Bucket}' создан.", bucket);
        }

        if (publicRead)
        {
            var policy = BuildPublicReadPolicy(bucket);
            await client.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy),
                cancellationToken);
        }
    }

    private static string BuildPublicReadPolicy(string bucket) =>
        $$"""
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Effect": "Allow",
              "Principal": "*",
              "Action": "s3:GetObject",
              "Resource": "arn:aws:s3:::{{bucket}}/*"
            }
          ]
        }
        """;
}
