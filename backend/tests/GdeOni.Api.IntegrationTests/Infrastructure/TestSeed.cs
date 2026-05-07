using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Хелперы для интеграционных тестов: создание умершего через at-grave
/// (с автотрекингом) и без burial-location (через regular Create), а также
/// загрузка валидного JPEG в MinIO.
/// </summary>
internal static class TestSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// POST /api/deceased-records/at-grave — создаёт Deceased + Tracking
    /// с координатами Москвы. Возвращает DeceasedId.
    /// </summary>
    public static async Task<Guid> CreateAtGraveAsync(
        HttpClient client,
        string? lastName = null)
    {
        var response = await client.PostAsJsonAsync("/api/deceased-records/at-grave", new
        {
            firstName = "Иван",
            lastName = lastName ?? $"Фам{Guid.NewGuid():N}",
            middleName = (string?)null,
            birthDate = (DateOnly?)null,
            deathDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)),
            shortDescription = (string?)null,
            biography = (string?)null,
            graveLocation = new
            {
                latitude = 55.7,
                longitude = 37.6,
                accuracyMeters = (double?)null,
                country = "Россия",
                city = "Москва",
                cemeteryName = "Test cemetery",
                plotNumber = (string?)null,
                graveNumber = (string?)null
            },
            tracking = new
            {
                relationshipType = (int)RelationshipType.Friend,
                personalNotes = (string?)null,
                notifyOnDeathAnniversary = false,
                notifyOnBirthAnniversary = false
            }
        });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"at-grave failed: {(int)response.StatusCode}. Body: {body}");
        }

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResultDto<AtGraveResultDto>>(JsonOptions);
        return payload!.Result!.DeceasedId;
    }

    /// <summary>
    /// POST /api/deceased-records (без at-grave) — создаёт Deceased
    /// без BurialLocation. Используется в сценарии "route → 409 если
    /// нет координат" и "DELETE burial-location → 409 если уже null".
    /// </summary>
    public static async Task<Guid> CreateWithoutBurialAsync(
        HttpClient client,
        string? lastName = null)
    {
        var response = await client.PostAsJsonAsync("/api/deceased-records", new
        {
            firstName = "Иван",
            lastName = lastName ?? $"NoBurial{Guid.NewGuid():N}",
            middleName = (string?)null,
            birthDate = (DateOnly?)null,
            deathDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)),
            shortDescription = (string?)null,
            biography = (string?)null,
            burialLocation = (object?)null
        });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"create without burial failed: {(int)response.StatusCode}. Body: {body}");
        }

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResultDto<CreateResultDto>>(JsonOptions);
        return payload!.Result!.Id;
    }

    /// <summary>
    /// Multipart upload валидного JPEG (FF D8 FF E0 + padding) с
    /// kind = DeceasedPhoto. Возвращает MediaId.
    /// </summary>
    public static async Task<Guid> UploadPhotoAsync(
        HttpClient client,
        Guid deceasedId,
        FileKind kind = FileKind.DeceasedPhoto)
    {
        using var multipart = BuildPhotoUpload(BuildJpegBytes(), "photo.jpg", kind);
        var response = await client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            multipart);

        // После D11.3.1 Upload возвращает 201 + Location.
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Upload failed {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResultDto<UploadResultDto>>(JsonOptions);
        return payload!.Result!.MediaId;
    }

    public static MultipartFormDataContent BuildPhotoUpload(
        byte[] bytes,
        string fileName,
        FileKind kind = FileKind.DeceasedPhoto)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", fileName);
        multipart.Add(new StringContent(((int)kind).ToString()), "kind");
        return multipart;
    }

    public static byte[] BuildJpegBytes()
    {
        // Magic bytes JPEG (FF D8 FF E0) + 12 байт под probe FileValidator.
        var bytes = new byte[16];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF; bytes[3] = 0xE0;
        return bytes;
    }

    private sealed class ApiResultDto<T>
    {
        public T? Result { get; set; }
    }

    private sealed class AtGraveResultDto
    {
        public Guid DeceasedId { get; set; }
    }

    private sealed class CreateResultDto
    {
        public Guid Id { get; set; }
    }

    private sealed class UploadResultDto
    {
        public Guid MediaId { get; set; }
    }
}
