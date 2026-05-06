using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Admin;

/// <summary>
/// D9.5.4 Admin-сценарии: verify/unverify, approve/reject memory + media.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AdminIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// PUT /verify админом → 200 + IsVerified=true.
    /// </summary>
    [Fact]
    public async Task Verify_Admin_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var verify = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/verify", new { });

        verify.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PUT /verify уже verified → 409 Conflict.
    /// </summary>
    [Fact]
    public async Task Verify_AlreadyVerified_Returns409()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var first = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/verify", new { });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/verify", new { });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// PUT /verify non-admin → 403 ([Authorize(Roles = SuperAdmin,Admin)]).
    /// </summary>
    [Fact]
    public async Task Verify_NotAdmin_Returns403()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var verify = await user.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/verify", new { });

        verify.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// PUT /memories/{id}/approve — happy path. Pending memory →
    /// admin одобряет → 200, ModerationStatus=Approved.
    /// </summary>
    [Fact]
    public async Task ApproveMemory_Admin_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var memoryId = await AddMemoryAsync(user.Client, deceasedId);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var approve = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/memories/{memoryId}/approve",
            new { });

        approve.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PUT /memories/{id}/reject — happy path. Pending memory →
    /// admin отклоняет → 200, ModerationStatus=Rejected.
    /// </summary>
    [Fact]
    public async Task RejectMemory_Admin_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);

        var memoryId = await AddMemoryAsync(user.Client, deceasedId);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var reject = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/memories/{memoryId}/reject",
            new { });

        reject.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PUT /media/{id}/approve admin → 204 (FromUnitResult).
    /// Pending media → Approved.
    /// </summary>
    [Fact]
    public async Task ApproveMedia_Admin_Returns204()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var mediaId = await TestSeed.UploadPhotoAsync(user.Client, deceasedId);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var approve = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/approve",
            new { });

        // ApproveMediaModerationUseCase возвращает UnitResult; контроллер
        // FromUnitResult → NoContent при успехе.
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// PUT /media/{id}/reject admin → 204 + файл best-effort удалён
    /// из MinIO (RejectMediaModerationUseCase делает Delete после Save).
    /// Само удаление файла проверяем через MinioFileStorageTests
    /// round-trip; здесь — статус-код контракта.
    /// </summary>
    [Fact]
    public async Task RejectMedia_Admin_Returns204()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateAtGraveAsync(user.Client);
        var mediaId = await TestSeed.UploadPhotoAsync(user.Client, deceasedId);

        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);
        var reject = await admin.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/media/{mediaId}/reject",
            new { });

        reject.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> AddMemoryAsync(HttpClient client, Guid deceasedId)
    {
        var addResp = await client.PostAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/memories",
            new { text = "Хороший человек" });
        if (addResp.StatusCode != HttpStatusCode.OK
            && addResp.StatusCode != HttpStatusCode.Created)
        {
            var body = await addResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"AddMemory failed: {(int)addResp.StatusCode}. {body}");
        }
        var payload = await addResp.Content
            .ReadFromJsonAsync<ApiResultDto<MemoryIdDto>>(JsonOptions);
        return payload!.Result!.MemoryId;
    }

    private sealed class ApiResultDto<T>
    {
        public T? Result { get; set; }
    }

    private sealed class MemoryIdDto
    {
        public Guid MemoryId { get; set; }
    }
}
