using GdeOni.Application.Abstractions.Storage;
using GdeOni.Domain.Shared;
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

    public async Task<StoredFile> UploadToBucketAsync(
        string bucket,
        string keyPrefix,
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = SanitizeExtension(originalFileName);
        var safePrefix = string.IsNullOrWhiteSpace(keyPrefix) ? "misc" : keyPrefix.Trim('/');
        var objectKey = $"{safePrefix}/{Guid.NewGuid()}{extension}";

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(sizeBytes)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, cancellationToken);

        return new StoredFile(
            bucket,
            objectKey,
            contentType,
            sizeBytes,
            originalFileName);
    }

    public async Task<StoredFile> CopyObjectAsync(
        string sourceBucket,
        string sourceObjectKey,
        string destBucket,
        string destKeyPrefix,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        var extension = SanitizeExtension(fileName);
        var safePrefix = string.IsNullOrWhiteSpace(destKeyPrefix) ? "misc" : destKeyPrefix.Trim('/');
        var destObjectKey = $"{safePrefix}/{Guid.NewGuid()}{extension}";

        var source = new CopySourceObjectArgs()
            .WithBucket(sourceBucket)
            .WithObject(sourceObjectKey);

        var args = new CopyObjectArgs()
            .WithBucket(destBucket)
            .WithObject(destObjectKey)
            .WithCopyObjectSource(source);

        await _client.CopyObjectAsync(args, cancellationToken);

        return new StoredFile(
            destBucket,
            destObjectKey,
            contentType,
            sizeBytes,
            fileName);
    }

    public Task<StoredFile> CopyObjectByKindAsync(
        string sourceBucket,
        string sourceObjectKey,
        MediaKind destKind,
        Guid deceasedId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        var destBucket = ResolveBucket(destKind);
        var keyPrefix = $"{destKind.ToString().ToLowerInvariant()}/{deceasedId}";
        return CopyObjectAsync(
            sourceBucket,
            sourceObjectKey,
            destBucket,
            keyPrefix,
            fileName,
            contentType,
            sizeBytes,
            cancellationToken);
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
        return $"{GetMediaBaseUrl()}/{bucket}/{Uri.EscapeDataString(objectKey)}";
    }

    public string GetMediaBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? $"{(_options.UseSsl ? "https" : "http")}://{_options.Endpoint}"
            : _options.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        // Clamp на MaxPresignedTtl (D11.6.3): даже если caller попросит
        // 30 дней, выдаём не более чем сконфигурированный максимум.
        var maxTtl = TimeSpan.FromHours(_options.MaxPresignedTtlHours);
        var effectiveTtl = expiresIn > maxTtl ? maxTtl : expiresIn;
        if (effectiveTtl <= TimeSpan.Zero)
            effectiveTtl = TimeSpan.FromMinutes(1);

        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry((int)effectiveTtl.TotalSeconds);

        var url = await _presignedClient.PresignedGetObjectAsync(args);

        // D11.6.4: если PublicBaseUrl содержит path-prefix
        // (https://files.example.com/storage), MinIO SDK теряет
        // /storage сегмент при формировании presigned URL — он
        // знает только host:port. Дописываем prefix вручную перед
        // первым "/" после хоста.
        return ApplyPublicPathPrefix(url, _options.PublicBaseUrl);
    }

    public async Task<DownloadedFile> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        // MinIO SDK даёт поток через callback. Заворачиваем в MemoryStream,
        // чтобы контроллер мог стримить клиенту независимо от того, дожил
        // ли исходный поток до конца. Для F13 это PDF до 25 MB — память
        // не критична. Большие файлы (если появятся) надо будет переделать
        // на pipe/PipeReader, чтобы не буферизировать.
        var ms = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(async (stream, ct) =>
            {
                await stream.CopyToAsync(ms, ct);
            });

        var stat = await _client.GetObjectAsync(args, cancellationToken);

        ms.Position = 0;
        // Stat иногда отдаёт ContentType пустой (SDK quirk на старых
        // версиях). В таком случае fallback на application/octet-stream
        // — браузер сам разберётся (для PDF mime обычно проставлен).
        var contentType = string.IsNullOrWhiteSpace(stat.ContentType)
            ? "application/octet-stream"
            : stat.ContentType;
        return new DownloadedFile(ms, contentType, stat.Size);
    }

    internal static string ApplyPublicPathPrefix(string presignedUrl, string? publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            return presignedUrl;
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri))
            return presignedUrl;
        if (!Uri.TryCreate(presignedUrl, UriKind.Absolute, out var signedUri))
            return presignedUrl;

        var prefix = publicUri.AbsolutePath.TrimEnd('/');
        if (prefix.Length == 0)
            return presignedUrl;

        // SDK уже выставил scheme/host из _presignedClient (host:port из
        // PublicBaseUrl). AbsolutePath даёт чистый path без query —
        // ручной split на '?' не нужен (D11.11.2).
        var builder = new UriBuilder(signedUri)
        {
            Path = prefix + signedUri.AbsolutePath,
            Query = signedUri.Query.TrimStart('?')
        };
        return builder.Uri.ToString();
    }

    private string ResolveBucket(MediaKind kind) => kind switch
    {
        MediaKind.DeceasedPhoto => _options.Buckets.DeceasedPhotos,
        MediaKind.GravePhoto => _options.Buckets.GravePhotos,
        MediaKind.Document => _options.Buckets.DeceasedDocuments,
        MediaKind.Other => _options.Buckets.DeceasedDocuments,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown media kind.")
    };

    // Whitelist расширений: всё, что не из этого набора, превращается
    // в ".bin". Защищает от user-controlled расширений вида ".jpg%00.exe"
    // или ".php" в storage-key (см. D11.6.1). Источник истины по
    // content-type валидации — FileValidator на Application-слое;
    // здесь — последний барьер именно в имени файла.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    private static string BuildObjectKey(UploadFileRequest request)
    {
        var extension = SanitizeExtension(request.OriginalFileName);
        var prefix = request.Kind.ToString().ToLowerInvariant();
        return $"{prefix}/{request.DeceasedId}/{Guid.NewGuid()}{extension}";
    }

    private static string SanitizeExtension(string? originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return ".bin";

        var raw = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(raw))
            return ".bin";

        // Path.GetExtension возвращает с ведущей точкой. Дополнительно
        // отбрасываем всё после первого "странного" символа и приводим
        // к lower — defense in depth, даже если хост-парсер пропустил
        // экзотику.
        var trimmed = new string(raw
            .TakeWhile(ch => char.IsLetterOrDigit(ch) || ch == '.')
            .ToArray());

        return AllowedExtensions.Contains(trimmed) ? trimmed.ToLowerInvariant() : ".bin";
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
