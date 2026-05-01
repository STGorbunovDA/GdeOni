using GdeOni.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace GdeOni.Infrastructure.Storage;

internal sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly IMinioClient _presignedClient;
    private readonly MinioOptions _options;

    public MinioFileStorage(IMinioClient client, IOptions<MinioOptions> options)
    {
        _client = client;
        _options = options.Value;
        _presignedClient = BuildPresignedClient(_options) ?? client;
    }

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

        await _client.PutObjectAsync(args, cancellationToken);

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

        return _client.RemoveObjectAsync(args, cancellationToken);
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

        return _presignedClient.PresignedGetObjectAsync(args);
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

    // Если PublicBaseUrl задан, presigned URL должен вести на публичный домен,
    // а не на внутренний minio:9000. Создаём отдельный клиент, у которого
    // endpoint = публичный host. PresignedGetObjectAsync генерит ссылку
    // локально (HMAC-подпись) — никаких сетевых вызовов, поэтому второй
    // клиент безопасен для use в Singleton MinioFileStorage.
    private static IMinioClient? BuildPresignedClient(MinioOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
            return null;

        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri))
            return null;

        var endpoint = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        var useSsl = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        var builder = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey);

        if (useSsl) builder = builder.WithSSL();

        return builder.Build();
    }
}
