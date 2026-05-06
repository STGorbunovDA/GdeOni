using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Api.IntegrationTests.Storage;

/// <summary>
/// Тесты <see cref="IFileStorage"/> (MinioFileStorage) на реальном
/// MinIO-контейнере из <see cref="GdeOniWebAppFactory"/>. WebAppFactory
/// уже поднимает MinIO и делает MinioBootstrap (создаёт buckets), поэтому
/// здесь мы просто берём IFileStorage из DI и проверяем round-trip.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MinioFileStorageTests
{
    private readonly GdeOniWebAppFactory _factory;

    public MinioFileStorageTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// Upload + GetPublicUrl + Delete round-trip. После Upload файл есть
    /// в MinIO; PublicUrl формируется без сетевых вызовов; Delete снимает
    /// файл, и повторное удаление не падает (MinIO сам идемпотентен).
    /// </summary>
    [Fact]
    public async Task Upload_GetPublicUrl_Delete_RoundTrip()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var request = new UploadFileRequest
        {
            Kind = FileKind.DeceasedPhoto,
            DeceasedId = Guid.NewGuid(),
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            SizeBytes = content.Length,
            Content = content
        };

        var stored = await storage.UploadAsync(request, CancellationToken.None);

        stored.Bucket.Should().Be("deceased-photos");
        stored.ObjectKey.Should().NotBeNullOrWhiteSpace();
        stored.ContentType.Should().Be("image/jpeg");
        stored.SizeBytes.Should().Be(content.Length);

        var publicUrl = storage.GetPublicUrl(stored.Bucket, stored.ObjectKey);
        publicUrl.Should().Contain(stored.Bucket);
        publicUrl.Should().Contain(Uri.EscapeDataString(stored.ObjectKey));

        // Delete первый раз — файл есть, удаляется без ошибок.
        await storage.DeleteAsync(stored.Bucket, stored.ObjectKey, CancellationToken.None);
    }

    /// <summary>
    /// GetPresignedUrlAsync формирует URL с подписью (HMAC) и сроком
    /// действия. Сетевых вызовов — нет, всё локально.
    /// </summary>
    [Fact]
    public async Task GetPresignedUrlAsync_ReturnsSignedUrl()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var url = await storage.GetPresignedUrlAsync(
            "deceased-documents",
            "test-key.pdf",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        url.Should().NotBeNullOrWhiteSpace();
        // Presigned URL содержит подпись и параметр expires.
        url.Should().Contain("X-Amz-Signature");
        url.Should().Contain("X-Amz-Expires");
    }

    /// <summary>
    /// DeleteAsync на несуществующий файл — не падает (MinIO Remove
    /// идемпотентен на уровне S3 API).
    /// </summary>
    [Fact]
    public async Task DeleteAsync_NonExistingFile_DoesNotThrow()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var act = () => storage.DeleteAsync(
            "deceased-photos",
            $"missing/{Guid.NewGuid()}.jpg",
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
