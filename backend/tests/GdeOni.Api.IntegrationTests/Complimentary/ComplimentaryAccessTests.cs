using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Api.IntegrationTests.Complimentary;

/// <summary>
/// D22. Integration-тесты на complimentary access:
/// admin grant/revoke + paywall-bypass.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ComplimentaryAccessTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ComplimentaryAccessTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// SubscriptionEnabled=true → юзер с истёкшей подпиской без complimentary
    /// должен получить 403. После grant'a — снова 200.
    /// </summary>
    [Fact]
    public async Task GrantedComplimentary_ExpiredUser_BypassesPaywall()
    {
        var factory = GateEnabledFactory();
        var user = await _factory.RegisterAndLoginAsync();
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        // Истекаем подписку юзера.
        await ExpireSubscriptionAsync(factory, user.Id);

        var userClient = factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);

        // Без complimentary — 403.
        var beforeGrant = await userClient.GetAsync("/api/deceased-records?pageSize=1");
        beforeGrant.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Админ выдаёт бесплатный доступ.
        var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var grantResp = await adminClient.PostAsJsonAsync(
            $"/api/admin/users/{user.Id}/complimentary-access",
            new { untilUtc = (DateTime?)null, note = "test promo" });
        grantResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // С complimentary — 200.
        var afterGrant = await userClient.GetAsync("/api/deceased-records?pageSize=1");
        afterGrant.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET /me/subscription отдаёт hasComplimentaryAccess=true.
        var sub = await userClient.GetAsync("/api/users/me/subscription");
        sub.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await sub.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("hasComplimentaryAccess").GetBoolean().Should().BeTrue();
        result.GetProperty("isActiveNow").GetBoolean().Should().BeTrue();
        result.GetProperty("complimentaryAccessNote").GetString().Should().Be("test promo");
    }

    /// <summary>
    /// После revoke юзер с истёкшей подпиской снова получает 403.
    /// </summary>
    [Fact]
    public async Task RevokedComplimentary_ExpiredUser_Returns403Again()
    {
        var factory = GateEnabledFactory();
        var user = await _factory.RegisterAndLoginAsync();
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        await ExpireSubscriptionAsync(factory, user.Id);

        var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        // Grant.
        var grantResp = await adminClient.PostAsJsonAsync(
            $"/api/admin/users/{user.Id}/complimentary-access",
            new { untilUtc = (DateTime?)null, note = (string?)null });
        grantResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Revoke.
        var revokeResp = await adminClient.DeleteAsync(
            $"/api/admin/users/{user.Id}/complimentary-access");
        revokeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var userClient = factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var resp = await userClient.GetAsync("/api/deceased-records?pageSize=1");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Юзер без admin-роли не может вызвать complimentary эндпоинты —
    /// 403 (по политике Roles="SuperAdmin,Admin").
    /// </summary>
    [Fact]
    public async Task NonAdminUser_GetsForbiddenOnComplimentaryEndpoints()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var target = await _factory.RegisterAndLoginAsync();

        var grantResp = await user.Client.PostAsJsonAsync(
            $"/api/admin/users/{target.Id}/complimentary-access",
            new { untilUtc = (DateTime?)null, note = (string?)null });
        grantResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revokeResp = await user.Client.DeleteAsync(
            $"/api/admin/users/{target.Id}/complimentary-access");
        revokeResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private WebApplicationFactory<Program> GateEnabledFactory() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:SubscriptionEnabled"] = "true",
                    ["FeatureFlags:GracePeriodDaysAfterExpiry"] = "0",
                });
            });
        });

    private static async Task ExpireSubscriptionAsync(
        WebApplicationFactory<Program> factory,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var dbUser = await repo.GetById(userId, default);
        dbUser!.ActivateSubscription(
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-30),
            $"pay-expired-{Guid.NewGuid():N}");
        dbUser.CancelSubscription(DateTime.UtcNow.AddDays(-29));
        await repo.Save(default);
    }
}
