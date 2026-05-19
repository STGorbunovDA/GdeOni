using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Payments;

/// <summary>
/// D16. Реальная интеграция с YooKassa API v3.
///
/// <para><b>CreateAsync:</b> <c>POST /v3/payments</c> с Basic-Auth
/// (<c>ShopId:SecretKey</c>) и заголовком <c>Idempotence-Key</c>
/// (GUID для повторных запросов).</para>
///
/// <para><b>VerifyWebhookAsync:</b> YooKassa по умолчанию НЕ подписывает
/// webhook'и HMAC'ом — рекомендация документации: после получения
/// уведомления сделать <c>GET /v3/payments/{id}</c> и сверить статус.
/// Подделать тело webhook'а можно, но подделать ответ YooKassa
/// API — нельзя. Защита от replay — идемпотентность на доменном
/// уровне (User.ActivateSubscription). IP whitelist делается на
/// уровне контроллера/middleware в D16.4.</para>
/// </summary>
public sealed class YooKassaPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _http;
    private readonly YooKassaOptions _options;
    private readonly ILogger<YooKassaPaymentProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public YooKassaPaymentProvider(
        HttpClient http,
        IOptions<YooKassaOptions> options,
        ILogger<YooKassaPaymentProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        ConfigureClient(_http, _options);
    }

    /// <summary>
    /// Применяет BaseAddress и Basic-Auth заголовок к HttpClient.
    /// Вынесено отдельно: вызывается и из конструктора, и из DI-фабрики
    /// (через AddHttpClient с custom настройкой).
    /// </summary>
    internal static void ConfigureClient(HttpClient http, YooKassaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey))
            return; // Fake-режим, реальный клиент не инициализирован.

        http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.ShopId}:{options.SecretKey}"));
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<Result<PaymentCreated, Error>> CreateAsync(
        Guid userId,
        decimal amountRub,
        string description,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            amount = new
            {
                value = amountRub.ToString("F2", CultureInfo.InvariantCulture),
                currency = "RUB"
            },
            // capture=true — однофазный платёж: оплачен → сразу
            // зачисляется на счёт магазина. Без capture YooKassa ставит
            // hold, и нужен отдельный API-вызов для финализации.
            capture = true,
            description,
            confirmation = new
            {
                type = "redirect",
                return_url = returnUrl
            },
            metadata = new
            {
                user_id = userId.ToString()
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/payments");
        httpRequest.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());
        httpRequest.Content = JsonContent.Create(request);

        try
        {
            var response = await _http.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "YooKassa.Create failed: status={Status} body={Body}",
                    (int)response.StatusCode, body);
                return Errors.General.Failure(
                    "payment.provider.create_failed",
                    $"YooKassa returned {(int)response.StatusCode}.");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var externalId = root.GetProperty("id").GetString() ?? string.Empty;
            var checkoutUrl = root
                .GetProperty("confirmation")
                .GetProperty("confirmation_url")
                .GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(checkoutUrl))
            {
                _logger.LogError("YooKassa.Create: malformed response body={Body}", body);
                return Errors.General.Failure(
                    "payment.provider.create_failed",
                    "YooKassa response missing id or confirmation_url.");
            }

            return Result.Success<PaymentCreated, Error>(
                new PaymentCreated(externalId, checkoutUrl));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "YooKassa.Create: HTTP error");
            return Errors.General.Failure(
                "payment.provider.network_error",
                "YooKassa is unreachable.");
        }
    }

    public async Task<Result<PaymentVerification, Error>> VerifyWebhookAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken)
    {
        // Шаг 1: парсим webhook payload — нам нужен только payment.id,
        // остальное мы запросим у YooKassa напрямую (защита от подделки).
        string externalId;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            externalId = doc.RootElement
                .GetProperty("object")
                .GetProperty("id")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YooKassa.Webhook: cannot parse payload");
            return Errors.Subscription.InvalidPaymentSignature();
        }

        if (string.IsNullOrWhiteSpace(externalId))
            return Errors.Subscription.InvalidPaymentSignature();

        // Шаг 2: верификация через pull. Делаем GET /v3/payments/{id} с
        // нашим SecretKey — если подделанный webhook прислал чужой
        // payment_id, YooKassa либо отдаст 404, либо реальный статус
        // (тогда мы примем правду). Это защищённее проверки HMAC,
        // потому что подделать ответ YooKassa нельзя.
        try
        {
            var response = await _http.GetAsync($"v3/payments/{externalId}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Errors.Subscription.PaymentNotFound();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "YooKassa.Get failed: status={Status} body={Body}",
                    (int)response.StatusCode, body);
                return Errors.General.Failure(
                    "payment.provider.verify_failed",
                    $"YooKassa GET returned {(int)response.StatusCode}.");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var statusString = root.GetProperty("status").GetString() ?? string.Empty;
            var status = MapStatus(statusString);

            var amountRub = decimal.Parse(
                root.GetProperty("amount").GetProperty("value").GetString() ?? "0",
                CultureInfo.InvariantCulture);

            return Result.Success<PaymentVerification, Error>(
                new PaymentVerification(externalId, status, amountRub));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "YooKassa.Get: HTTP error");
            return Errors.General.Failure(
                "payment.provider.network_error",
                "YooKassa is unreachable.");
        }
    }

    public Task<UnitResult<Error>> CancelRecurringAsync(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        // Single-payment модель — no-op на YooKassa-стороне. Если когда-нибудь
        // подключим saved-card auto-charge, здесь будет DELETE recurring
        // или эквивалент.
        _logger.LogInformation(
            "YooKassa.CancelRecurring: paymentId={PaymentId} (no-op, single-payment model)",
            externalPaymentId);
        return Task.FromResult(UnitResult.Success<Error>());
    }

    /// <summary>
    /// Маппит строку статуса YooKassa в наш <see cref="PaymentStatus"/>.
    /// <list type="bullet">
    ///   <item><c>succeeded</c> → <see cref="PaymentStatus.Succeeded"/>;</item>
    ///   <item><c>canceled</c> → <see cref="PaymentStatus.Cancelled"/>;</item>
    ///   <item><c>pending</c>, <c>waiting_for_capture</c>, остальное → <see cref="PaymentStatus.Pending"/>.</item>
    /// </list>
    /// </summary>
    internal static PaymentStatus MapStatus(string yooKassaStatus) =>
        yooKassaStatus switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending,
        };
}
