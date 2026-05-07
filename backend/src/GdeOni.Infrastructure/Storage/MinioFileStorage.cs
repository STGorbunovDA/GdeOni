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
