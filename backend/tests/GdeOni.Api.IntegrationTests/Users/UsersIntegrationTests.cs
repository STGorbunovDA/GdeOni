using System.Net;
using System.Net.Http.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Users;

/// <summary>
/// D9.5.4 Users-сценарии: GET /api/users только админам, PATCH self,
/// PATCH чужого без admin → 403, PUT email отзывает RT, DELETE self → 403.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class UsersIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    public UsersIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// GET /api/users без admin-роли → 403.
    /// [Authorize(Roles = "SuperAdmin,Admin")] на эндпоинте.
    /// </summary>
    [Fact]
    public async Task GetAll_NotAdmin_Returns403()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// GET /api/users с admin-ролью → 200.
    /// </summary>
    [Fact]
    public async Task GetAll_Admin_Returns200()
    {
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        var response = await admin.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PATCH /api/users/{id} self → 200. Юзер обновляет свой профиль.
    /// </summary>
    [Fact]
    public async Task UpdateProfile_Self_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PatchAsJsonAsync($"/api/users/{user.Id}", new
        {
            userName = $"new-{Guid.NewGuid():N}",
            fullName = "Иванов Иван Иванович"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PATCH /api/users/{id} чужого без admin → 403 + user.forbidden.
    /// Errors.User.UserForbidden() = Error.Forbidden, маппится в 403
    /// через ResponseExtensions.ToErrorResponse.
    /// </summary>
    [Fact]
    public async Task UpdateProfile_Foreign_NotAdmin_Returns403()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var bob = await _factory.RegisterAndLoginAsync();

        var response = await alice.Client.PatchAsJsonAsync($"/api/users/{bob.Id}", new
        {
            userName = $"hk{Guid.NewGuid():N}"[..16],
            fullName = "Иванов"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"user.forbidden\"");
    }

    /// <summary>
    /// PUT /api/users/{id}/email — после смены email старые refresh-токены
    /// инвалидируются (см. ChangeEmailUseCase: RevokeAllForUser после Save).
    /// </summary>
    [Fact]
    public async Task ChangeEmail_RevokesOldRefreshTokens()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var oldRefresh = user.RefreshToken;

        var changeResponse = await user.Client.PutAsJsonAsync($"/api/users/{user.Id}/email", new
        {
            email = $"new-{Guid.NewGuid():N}@example.com"
        });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Старый refresh — после ChangeEmail revoked.
        var refresh = await user.Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefresh
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// DELETE /api/users/{id} self → 403 (DeleteSelfForbidden даже для admin).
    /// Оба слоя защиты: [Authorize(Roles=Admin)] плюс самопроверка.
    /// </summary>
    [Fact]
    public async Task Delete_Self_Returns403()
    {
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        var response = await admin.Client.DeleteAsync($"/api/users/{admin.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
