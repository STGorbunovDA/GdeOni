using GdeOni.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace GdeOni.Infrastructure.Storage;

internal sealed class MinioFileStorage(
    IMinioClient client,
    IOptions<MinioOptions> options)
    : IFileStorage
{
    private readonly MinioOptions _options = options.Value;

    public async Task<StoredFile> UploadAsync(
        UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        var bucket = ResolveBucket(request.Kind);
        var objectKey = BuildObjectKey(request);

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(request.Content)
            .WithObjectSize(request.SizeBytes)
            .WithContentType(request.ContentType);

        await client.PutObjectAsync(args, cancellationToken);

        return new StoredFile(
            bucket,
            objectKey,
            request.ContentType,
            request.SizeBytes,
            request.OriginalFileName);
    }

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey);

        return client.RemoveObjectAsync(args, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        var memory = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memory));

        await client.GetObjectAsync(args, cancellationToken);

        memory.Position = 0;
        return memory;
    }

    public string GetPublicUrl(string bucket, string objectKey)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? $"{(_options.UseSsl ? "https" : "http")}://{_options.Endpoint}"
            : _options.PublicBaseUrl.TrimEnd('/');

        return $"{baseUrl}/{bucket}/{Uri.EscapeDataString(objectKey)}";
    }

    public Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry((int)expiresIn.TotalSeconds);

        return client.PresignedGetObjectAsync(args);
    }

    private string ResolveBucket(FileKind kind) => kind switch
    {
        FileKind.DeceasedPhoto => _options.Buckets.DeceasedPhotos,
        FileKind.GravePhoto => _options.Buckets.GravePhotos,
        FileKind.Document => _options.Buckets.DeceasedDocuments,
        FileKind.Other => _options.Buckets.DeceasedDocuments,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind.")
    };

    private static string BuildObjectKey(UploadFileRequest request)
    {
        var extension = Path.GetExtension(request.OriginalFileName);
        var prefix = request.Kind.ToString().ToLowerInvariant();
        return $"{prefix}/{request.DeceasedId}/{Guid.NewGuid()}{extension}";
    }
}
