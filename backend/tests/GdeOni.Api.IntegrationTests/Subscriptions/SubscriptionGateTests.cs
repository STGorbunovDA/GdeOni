using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Api.IntegrationTests.Subscriptions;

/// <summary>
/// D16.5. Тесты глобального гейта <c>RequireActiveSubscription</c>.
///
/// Через <see cref="WebApplicationFactory{Program}.WithWebHostBuilder"/>
/// переопределяем <c>FeatureFlags:SubscriptionEnabled=true</c>, что
/// активирует <see cref="ActiveSubscriptionAuthorizationHandler"/>.
/// Без этого override все integration-тесты остаются на свободном
/// доступе (что и нужно для прохода исторических 91 тестов).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SubscriptionGateTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubscriptionGateTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// Фабрика с SubscriptionEnabled=true. Тесты в этом классе должны
    /// получать 403 на gated-эндпоинтах для юзеров без активной подписки.
    /// </summary>
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

    /// <summary>
    /// Юзер на Trial — гейт пропускает. Запрос на DeceasedRecords (gated)
    /// → 200.
    /// </summary>
    [Fact]
    public async Task GatedEndpoint_UserOnTrial_Returns200()
    {
        var factory = GateEnabledFactory();
        var user = await _factory.RegisterAndLoginAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var response = await client.GetAsync("/api/deceased-records?pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Юзер с истёкшим Trial → 403 + errorCode subscription.required.
    /// Истечение симулируем прямо в БД через scope: меняем
    /// ExpiresAtUtc в прошлое.
    /// </summary>
    [Fact]
    public async Task GatedEndpoint_TrialExpired_Returns403WithSubscriptionRequired()
    {
        var factory = GateEnabledFactory();
        var user = await _factory.RegisterAndLoginAsync();

        // Истекаем trial-период у юзера через прямую мутацию БД.
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var dbUser = await repo.GetById(user.Id, default);
            dbUser!.ActivateSubscription(
                SubscriptionPlan.Monthly,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-30),
                "pay-expired");
            // Сразу cancel, чтобы остался Cancelled с истёкшим ExpiresAt.
            dbUser.CancelSubscription(DateTime.UtcNow.AddDays(-29));
            await repo.Save(default);
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var response = await client.GetAsync("/api/deceased-records?pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("subscription.required");
    }

    /// <summary>
    /// Whitelist'овый эндпоинт /api/users/me доступен даже с истёкшим
    /// Trial — иначе юзер заперт в ловушке "не могу зайти в профиль
    /// чтобы оплатить".
    /// </summary>
    [Fact]
    public async Task WhitelistedEndpoint_TrialExpired_Returns200()
    {
        var factory = GateEnabledFactory();
        var user = await _factory.RegisterAndLoginAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var dbUser = await repo.GetById(user.Id, default);
            dbUser!.ActivateSubscription(
                SubscriptionPlan.Monthly,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-30),
                "pay-expired-2");
            dbUser.CancelSubscription(DateTime.UtcNow.AddDays(-29));
            await repo.Save(default);
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);

        // /me — whitelisted (BasicAuthenticated policy).
        var meResp = await client.GetAsync("/api/users/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // SubscriptionsController — тоже whitelisted (нельзя гейтить
        // вход в подписку самой подпиской).
        var subResp = await client.GetAsync("/api/users/me/subscription");
        subResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // /api/app/features — тоже whitelisted, paywall его читает.
        var featuresResp = await client.GetAsync("/api/app/features");
        featuresResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Admin / SuperAdmin освобождён от подписки даже когда гейт активен.
    /// Решение 2026-05-14.
    /// </summary>
    [Fact]
    public async Task GatedEndpoint_AdminWithoutSubscription_Returns200()
    {
        var factory = GateEnabledFactory();
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        // Истекаем у админа подписку — это не должно влиять на доступ
        // т.к. handler bypass'ит по роли из claim'а.
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var dbAdmin = await repo.GetById(admin.Id, default);
            dbAdmin!.ActivateSubscription(
                SubscriptionPlan.Monthly,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-30),
                "pay-admin");
            dbAdmin.CancelSubscription(DateTime.UtcNow.AddDays(-29));
            await repo.Save(default);
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await client.GetAsync("/api/deceased-records?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// SubscriptionEnabled=false (open-beta) — гейт не активен,
    /// юзеру без подписки можно ходить на gated-эндпоинты.
    /// </summary>
    [Fact]
    public async Task GatedEndpoint_SubscriptionDisabled_Returns200()
    {
        // Дефолтная factory (SubscriptionEnabled=false).
        var user = await _factory.RegisterAndLoginAsync();

        // Прямо истекаем подписку — но т.к. флаг выключен, handler
        // Succeed'ит без проверки.
        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var dbUser = await repo.GetById(user.Id, default);
            dbUser!.ActivateSubscription(
                SubscriptionPlan.Monthly,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-30),
                $"pay-disabled-{Guid.NewGuid():N}");
            dbUser.CancelSubscription(DateTime.UtcNow.AddDays(-29));
            await repo.Save(default);
        }

        var response = await user.Client.GetAsync("/api/deceased-records?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
