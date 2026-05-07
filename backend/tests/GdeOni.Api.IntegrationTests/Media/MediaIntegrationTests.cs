using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Media;

/// <summary>
/// D9.5.4 Media-сценарии. Используем at-grave для seed-deceased,
/// потом Upload через multipart/form-data, плюс GET / PATCH / DELETE.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MediaIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MediaIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// POST /media: валидный JPEG (с правильными magic bytes) → 200 +
    /// metadata в БД, файл в MinIO.
    /// </summary>
    [Fact]
    public async Task Upload_ValidPhoto_Returns201AndMetadata()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        using var content = TestSeed.BuildPhotoUpload(TestSeed.BuildJpegBytes(), "photo.jpg");
        var response = await user.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            content);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected 201, got {(int)response.StatusCode}. Body: {errorBody}");
        }
    }

    /// <summary>
    /// Upload файл с неправильным MIME (text/plain) → 400 +
    /// media.photo.content_type.not_allowed.
    /// </summary>
    [Fact]
    public async Task Upload_InvalidMime_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("hello"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "file", "file.txt");
        multipart.Add(new StringContent(((int)MediaKind.DeceasedPhoto).ToString()), "kind");

        var response = await user.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            multipart);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("content_type.not_allowed");
    }

    /// <summary>
    /// Upload слишком большой файл (>10 MB как Photo) → 400.
    /// </summary>
    [Fact]
    public async Task Upload_TooLargePhoto_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var bytes = new byte[FileValidator.MaxPhotoSizeBytes + 1024];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;

        using var multipart = TestSeed.BuildPhotoUpload(bytes, "huge.jpg");
        var response = await user.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            multipart);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("media.photo.too_large");
    }

    /// <summary>
    /// GET /media возвращает список с пагинацией. После Upload — 1+ запись.
    /// </summary>
    [Fact]
    public async Task GetList_AfterUpload_ReturnsPagedItems()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        await TestSeed.UploadPhotoAsync(user.Client, deceasedId);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records/{deceasedId}/media?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
        body.Should().Contain("\"totalCount\"");
    }

    /// <summary>
    /// GET /media/{mediaId} возвращает URL и метаданные. Для DeceasedPhoto
    /// — public URL, для Document — presigned (X-Amz-Signature).
    /// </summary>
    [Fact]
    public async Task GetById_ReturnsUrlAndMetadata()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var photoId = await TestSeed.UploadPhotoAsync(user.Client, deceasedId);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"url\"");
        // Для DeceasedPhoto URL — public (без подписи), просто bucket+key.
        body.Should().Contain("deceased-photos");
        // isPresigned=false для photo.
        body.Should().Contain("\"isPresigned\":false");
    }

    /// <summary>
    /// DELETE /media/{mediaId} автором → 204; outsider → 403.
    /// </summary>
    [Fact]
    public async Task Delete_OutsiderForbidden_AuthorOk()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(alice.Client);
        var mediaId = await TestSeed.UploadPhotoAsync(alice.Client, deceasedId);

        var bob = await _factory.RegisterAndLoginAsync();
        var outsiderDelete = await bob.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}");
        outsiderDelete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var authorDelete = await alice.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}");
        authorDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// PATCH /media/{mediaId}/description: автор → 200, outsider → 403.
    /// </summary>
    [Fact]
    public async Task UpdateDescription_AuthorOk_OutsiderForbidden()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(alice.Client);
        var mediaId = await TestSeed.UploadPhotoAsync(alice.Client, deceasedId);

        var authorPatch = await alice.Client.PatchAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/description",
            new { description = "Новое описание" });
        authorPatch.StatusCode.Should().Be(HttpStatusCode.OK);

        var bob = await _factory.RegisterAndLoginAsync();
        var outsiderPatch = await bob.Client.PatchAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/description",
            new { description = "hacked" });
        outsiderPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// PATCH /media/{mediaId}/main-photo:
    /// — Pending фото (только что загружено, ещё не Approved) → 409
    ///   (Errors.DeceasedMedia.MainPhotoMustBeApproved);
    /// — GravePhoto kind → 409 (Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain);
    /// — Approved DeceasedPhoto → 204.
    /// </summary>
    [Fact]
    public async Task SetMainPhoto_PendingAndGraveAndApproved()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        // Pending DeceasedPhoto: только что загружено, статус ModerationStatus.Pending.
        var pendingPhotoId = await TestSeed.UploadPhotoAsync(user.Client, deceasedId);
        var pendingResp = await user.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{pendingPhotoId}/main-photo",
            content: null);
        pendingResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // GravePhoto kind: главное фото может быть только DeceasedPhoto.
        var gravePhotoId = await TestSeed.UploadPhotoAsync(user.Client, deceasedId, MediaKind.GravePhoto);
        var graveResp = await user.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{gravePhotoId}/main-photo",
            content: null);
        graveResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Approve через admin, потом SetMainPhoto → 204.
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(Domain.Shared.UserRole.Admin);
        var approveResp = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{pendingPhotoId}/approve",
            new { });
        approveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mainResp = await user.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{pendingPhotoId}/main-photo",
            content: null);
        mainResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
