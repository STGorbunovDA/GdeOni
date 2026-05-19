using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Subscriptions;

/// <summary>
/// D16. End-to-end тесты на SubscriptionsController и PaymentsController.
/// Используется FakePaymentProvider (в тестовом factory нет
/// YooKassa-ключей), что покрывает реальный путь register → Trial →
/// CreatePayment (PendingPayment) → webhook (Active).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SubscriptionsControllerTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubscriptionsControllerTests(GdeOniWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMy_Anonymous_Returns401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/users/me/subscription");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// После регистрации юзер сразу на Trial с DaysUntilExpiry ≈ 30.
    /// </summary>
    [Fact]
    public async Task GetMy_FreshRegistered_ReturnsTrial()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/users/me/subscription");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("status").GetString().Should().Be("Trial");
        result.GetProperty("isActiveNow").GetBoolean().Should().BeTrue();
        result.GetProperty("isOnTrial").GetBoolean().Should().BeTrue();
        result.GetProperty("daysUntilExpiry").GetInt32().Should().BeInRange(28, 30);
    }

    [Fact]
    public async Task CreatePayment_ValidPlan_ReturnsCheckoutUrl()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/users/me/subscription/create-payment",
            new { plan = "Monthly" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("checkoutUrl").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("externalPaymentId").GetString().Should().StartWith("fake-");
    }

    /// <summary>
    /// После CreatePayment Status = PendingPayment, но Trial ещё активен —
    /// IsActiveNow остаётся true (юзер пользуется приложением пока
    /// webhook идёт).
    /// </summary>
    [Fact]
    public async Task CreatePayment_ThenGetMy_StatusIsPendingPayment()
    {
        var user = await _factory.RegisterAndLoginAsync();
        await user.Client.PostAsJsonAsync(
            "/api/users/me/subscription/create-payment",
            new { plan = "Monthly" });

        var response = await user.Client.GetAsync("/api/users/me/subscription");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("status").GetString().Should().Be("PendingPayment");
        result.GetProperty("isActiveNow").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// Полный e2e: register → CreatePayment → симулируем YooKassa-webhook
    /// → Status становится Active.
    /// </summary>
    [Fact]
    public async Task FullFlow_Register_CreatePayment_Webhook_BecomesActive()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var createResp = await user.Client.PostAsJsonAsync(
            "/api/users/me/subscription/create-payment",
            new { plan = "Monthly" });
        createResp.EnsureSuccessStatusCode();

        var createBody = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var paymentId = createDoc.RootElement
            .GetProperty("result")
            .GetProperty("externalPaymentId")
            .GetString();

        // Симулируем webhook (FakePaymentProvider парсит externalPaymentId
        // прямо из тела и сразу возвращает Succeeded). Идёт от
        // анонимного клиента — webhook без auth.
        var anonymous = _factory.CreateClient();
        var webhookPayload = $$"""{"externalPaymentId":"{{paymentId}}"}""";

        var webhookResp = await anonymous.PostAsync(
            "/api/payments/yookassa/webhook",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));
        webhookResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meResp = await user.Client.GetAsync("/api/users/me/subscription");
        var meBody = await meResp.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meBody);
        var status = meDoc.RootElement.GetProperty("result").GetProperty("status").GetString();
        status.Should().Be("Active");
    }

    [Fact]
    public async Task Webhook_InvalidPayload_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync(
            "/api/payments/yookassa/webhook",
            new StringContent("not valid json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_UnknownPaymentId_Returns404()
    {
        var anonymous = _factory.CreateClient();
        var payload = """{"externalPaymentId":"fake-does-not-exist-anywhere"}""";

        var response = await anonymous.PostAsync(
            "/api/payments/yookassa/webhook",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_OnTrial_Returns204AndStatusIsCancelled()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var cancelResp = await user.Client.PostAsync(
            "/api/users/me/subscription/cancel", content: null);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meResp = await user.Client.GetAsync("/api/users/me/subscription");
        var meBody = await meResp.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meBody);
        meDoc.RootElement.GetProperty("result")
            .GetProperty("status").GetString().Should().Be("Cancelled");
    }
}
