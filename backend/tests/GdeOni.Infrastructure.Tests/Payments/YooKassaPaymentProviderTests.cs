using System.Net;
using System.Text;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Tests.Payments;

/// <summary>
/// D16. Тесты <see cref="YooKassaPaymentProvider"/> через
/// <see cref="StubHttpMessageHandler"/> — HttpClient мокируется на
/// уровне HttpMessageHandler, чтобы не дёргать реальную YooKassa.
/// </summary>
public sealed class YooKassaPaymentProviderTests
{
    private static readonly YooKassaOptions DefaultOptions = new()
    {
        BaseUrl = "https://api.yookassa.ru",
        ShopId = "1359063",
        SecretKey = "test_secret_key",
    };

    [Fact]
    public async Task CreateAsync_HappyPath_ParsesResponseAndReturnsCheckoutUrl()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/v3/payments");
            request.Headers.Authorization!.Scheme.Should().Be("Basic");
            request.Headers.Contains("Idempotence-Key").Should().BeTrue();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                    "id": "23d93cac-000f-5000-8000-126628f15141",
                    "status": "pending",
                    "amount": { "value": "49.00", "currency": "RUB" },
                    "confirmation": {
                        "type": "redirect",
                        "confirmation_url": "https://yoomoney.ru/checkout/payments/v2/contract?orderId=23d93cac"
                    }
                }
                """, Encoding.UTF8, "application/json")
            };
        });

        var sut = BuildProvider(handler);

        var result = await sut.CreateAsync(
            Guid.NewGuid(), 49m, "Подписка", "https://gdeoni.ru/return", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalPaymentId.Should().Be("23d93cac-000f-5000-8000-126628f15141");
        result.Value.CheckoutUrl.Should().Contain("yoomoney.ru/checkout");
    }

    [Fact]
    public async Task CreateAsync_YooKassaReturns400_ReturnsFailure()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"type\":\"error\"}")
            });

        var sut = BuildProvider(handler);

        var result = await sut.CreateAsync(
            Guid.NewGuid(), 49m, "Подписка", "https://gdeoni.ru/return", default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.provider.create_failed");
    }

    [Fact]
    public async Task VerifyWebhookAsync_SucceededStatus_ReturnsSucceeded()
    {
        // Webhook payload — небольшой формат YooKassa: { event, object }.
        // Парсим из него только object.id, а затем дёргаем GET для
        // финального статуса.
        var webhookPayload = """
            {
                "event": "payment.succeeded",
                "object": { "id": "23d93cac-000f-5000-8000-126628f15141" }
            }
            """;

        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/v3/payments/23d93cac-000f-5000-8000-126628f15141");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                    "id": "23d93cac-000f-5000-8000-126628f15141",
                    "status": "succeeded",
                    "amount": { "value": "49.00", "currency": "RUB" }
                }
                """)
            };
        });

        var sut = BuildProvider(handler);

        var result = await sut.VerifyWebhookAsync(webhookPayload, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalPaymentId.Should().Be("23d93cac-000f-5000-8000-126628f15141");
        result.Value.Status.Should().Be(PaymentStatus.Succeeded);
        result.Value.AmountRub.Should().Be(49m);
    }

    [Fact]
    public async Task VerifyWebhookAsync_MalformedPayload_ReturnsInvalidSignature()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            Assert.Fail("HTTP не должно вызываться при невалидном payload.");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var sut = BuildProvider(handler);

        var result = await sut.VerifyWebhookAsync("{this is not json", null, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.invalid_signature");
    }

    [Fact]
    public async Task VerifyWebhookAsync_YooKassaReturns404_ReturnsPaymentNotFound()
    {
        var webhookPayload = """
            { "object": { "id": "fake-id-that-does-not-exist" } }
            """;

        var handler = new StubHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("")
            });

        var sut = BuildProvider(handler);

        var result = await sut.VerifyWebhookAsync(webhookPayload, null, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.not_found");
    }

    [Theory]
    [InlineData("succeeded", PaymentStatus.Succeeded)]
    [InlineData("canceled", PaymentStatus.Cancelled)]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("waiting_for_capture", PaymentStatus.Pending)]
    [InlineData("anything_else", PaymentStatus.Pending)]
    public void MapStatus_MapsYooKassaStatusCorrectly(string yooKassaStatus, PaymentStatus expected)
    {
        YooKassaPaymentProvider.MapStatus(yooKassaStatus).Should().Be(expected);
    }

    private static YooKassaPaymentProvider BuildProvider(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler);
        YooKassaPaymentProvider.ConfigureClient(http, DefaultOptions);
        return new YooKassaPaymentProvider(
            http,
            Options.Create(DefaultOptions),
            NullLogger<YooKassaPaymentProvider>.Instance);
    }
}

/// <summary>
/// Stub <see cref="HttpMessageHandler"/> для unit-тестов: принимает
/// функцию (request, ct) → response, без сетевых вызовов.
/// </summary>
internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(responder(request, cancellationToken));
    }
}
