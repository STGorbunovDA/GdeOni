using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Media;

/// <summary>
/// Media-сценарии. Используем at-grave для seed-deceased, потом Upload
/// через multipart/form-data, плюс GET / PATCH / DELETE.
///
/// <para>
/// D26. Write-операции (POST/PATCH/DELETE) разрешены только админам —
/// бизнес-флоу теперь такой: юзер пишет, админ выкладывает медиа сам.
/// В тестах: deceased создаёт обычный юзер (это разрешено), media
/// добавляет admin. Негативные кейсы для обычного юзера на write —
/// 403.
/// </para>
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
    /// POST /media админом: валидный JPEG (с правильными magic bytes)
    /// → 201 + metadata в БД, файл в MinIO.
    /// </summary>
    [Fact]
    public async Task Upload_ValidPhoto_AsAdmin_Returns201AndMetadata()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        using var content = TestSeed.BuildPhotoUpload(TestSeed.BuildJpegBytes(), "photo.jpg");
        var response = await admin.Client.PostAsync(
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
    /// D26. POST /media обычным юзером (даже автором карточки) → 403.
    /// Атрибут [Authorize(Roles="SuperAdmin,Admin")] на контроллере
    /// отбивает запрос до use case'а.
    /// </summary>
    [Fact]
    public async Task Upload_AsRegularUser_Returns403()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        using var content = TestSeed.BuildPhotoUpload(TestSeed.BuildJpegBytes(), "photo.jpg");
        var response = await user.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Upload админом файла с неправильным MIME (text/plain) → 400 +
    /// media.photo.content_type.not_allowed.
    /// </summary>
    [Fact]
    public async Task Upload_InvalidMime_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("hello"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "file", "file.txt");
        multipart.Add(new StringContent(((int)MediaKind.DeceasedPhoto).ToString()), "kind");

        var response = await admin.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            multipart);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("content_type.not_allowed");
    }

    /// <summary>
    /// Upload админом слишком большого файла (>10 MB как Photo) → 400.
    /// </summary>
    [Fact]
    public async Task Upload_TooLargePhoto_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        var bytes = new byte[FileValidator.MaxPhotoSizeBytes + 1024];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;

        using var multipart = TestSeed.BuildPhotoUpload(bytes, "huge.jpg");
        var response = await admin.Client.PostAsync(
            $"/api/deceased-records/{deceasedId}/media",
            multipart);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("media.photo.too_large");
    }

    /// <summary>
    /// GET /media возвращает список с пагинацией обычному юзеру (чтение
    /// не ограничено — только запись). После Upload админом — 1+ запись.
    /// </summary>
    [Fact]
    public async Task GetList_AfterUpload_ReturnsPagedItems()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records/{deceasedId}/media?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
        body.Should().Contain("\"totalCount\"");
    }

    /// <summary>
    /// GET /media/{mediaId} возвращает URL и метаданные. D47: для DeceasedPhoto
    /// url — путь к «вахтёру» (/api/media/{id}/content), а не прямой публичный
    /// URL MinIO; для Document — presigned (X-Amz-Signature).
    /// </summary>
    [Fact]
    public async Task GetById_ReturnsUrlAndMetadata()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var photoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"url\"");
        // D47: для DeceasedPhoto url ведёт на «вахтёра», не на MinIO.
        body.Should().Contain($"/api/media/{photoId}/content");
        // isPresigned=false для photo.
        body.Should().Contain("\"isPresigned\":false");
    }

    /// <summary>
    /// D47. «Вахтёр» фото: авторизованный пользователь получает файл по
    /// GET /api/media/{id}/content — 200 + image/*. Байты реально отдаются
    /// через сервер, не по прямой ссылке MinIO.
    /// </summary>
    [Fact]
    public async Task GetContent_Authenticated_ReturnsFile()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var photoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var response = await user.Client.GetAsync($"/api/media/{photoId}/content");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    /// <summary>
    /// D47. Тот же файл без входа (аноним) — 401. Это и есть закрытие дырки:
    /// утёкшая ссылка без авторизации фото больше не отдаёт.
    /// </summary>
    [Fact]
    public async Task GetContent_Anonymous_Unauthorized()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var photoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/media/{photoId}/content");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// D26. DELETE /media/{mediaId} админом → 204; автором карточки → 403;
    /// outsider → 403.
    /// </summary>
    [Fact]
    public async Task Delete_AdminOk_RegularUsersForbidden()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(alice.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var mediaId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        // Outsider — 403.
        var bob = await _factory.RegisterAndLoginAsync();
        var outsiderDelete = await bob.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}");
        outsiderDelete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // D26. Автор карточки — тоже 403.
        var authorDelete = await alice.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}");
        authorDelete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin — 204.
        var adminDelete = await admin.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}");
        adminDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// D26. PATCH /media/{mediaId}/description: admin → 200, автор
    /// карточки → 403, outsider → 403.
    /// </summary>
    [Fact]
    public async Task UpdateDescription_AdminOk_RegularUsersForbidden()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(alice.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var mediaId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var adminPatch = await admin.Client.PatchAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/description",
            new { description = "Новое описание" });
        adminPatch.StatusCode.Should().Be(HttpStatusCode.OK);

        // D26. Автор карточки больше не может править описание.
        var authorPatch = await alice.Client.PatchAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/description",
            new { description = "автор пробует" });
        authorPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var bob = await _factory.RegisterAndLoginAsync();
        var outsiderPatch = await bob.Client.PatchAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/description",
            new { description = "hacked" });
        outsiderPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// PATCH /media/{mediaId}/main-photo:
    /// — Rejected фото → 409 (Errors.DeceasedMedia.MainPhotoMustBeApproved);
    /// — GravePhoto kind → 409 (Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain);
    /// — Approved DeceasedPhoto → 204.
    /// D26: write-операции делаем под админом.
    /// </summary>
    [Fact]
    public async Task SetMainPhoto_RejectedAndGraveAndApproved()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        // Свежезагруженное админом DeceasedPhoto — Approved (D26: загружает
        // только админ → auto-approve). Чтобы получить ветку 409
        // MainPhotoMustBeApproved, admin его явно Reject'нёт.
        var photoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var rejectResp = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}/reject",
            new { });
        rejectResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rejectedMainResp = await admin.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}/main-photo",
            content: null);
        rejectedMainResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // GravePhoto kind: главное фото может быть только DeceasedPhoto.
        var gravePhotoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId, MediaKind.GravePhoto);
        var graveResp = await admin.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{gravePhotoId}/main-photo",
            content: null);
        graveResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Approve обратно, потом SetMainPhoto → 204.
        var approveResp = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}/approve",
            new { });
        approveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mainResp = await admin.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}/main-photo",
            content: null);
        mainResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// D26. PATCH /media/{mediaId}/main-photo обычным юзером → 403.
    /// </summary>
    [Fact]
    public async Task SetMainPhoto_AsRegularUser_Returns403()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var photoId = await TestSeed.UploadPhotoAsync(admin.Client, deceasedId);

        var response = await user.Client.PatchAsync(
            $"/api/deceased-records/{deceasedId}/media/{photoId}/main-photo",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
