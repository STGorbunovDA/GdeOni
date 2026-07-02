using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Infrastructure.Payments;

/// <summary>
/// D16. Заглушка <see cref="IPaymentProvider"/> для dev и integration-тестов.
/// <see cref="CreateAsync"/> генерирует случайный GUID-paymentId и
/// фейковый CheckoutUrl. <see cref="VerifyWebhookAsync"/> ничего не
/// проверяет — тесты вызывают эндпоинт с произвольным payload и
/// получают <see cref="PaymentStatus.Succeeded"/>.
///
/// Регистрируется как <see cref="IPaymentProvider"/>, если в
/// appsettings не задана секция <c>YooKassa</c> с непустым SecretKey
/// (см. <see cref="GdeOni.Infrastructure.DependencyInjection"/>).
/// </summary>
public sealed class FakePaymentProvider(
    ILogger<FakePaymentProvider> logger) : IPaymentProvider
{
    public Task<Result<PaymentCreated, Error>> CreateAsync(
        Guid userId,
        decimal amountRub,
        string description,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var paymentId = $"fake-{Guid.NewGuid():N}";
        var checkoutUrl = $"https://example.invalid/fake-checkout/{paymentId}";

        logger.LogInformation(
            "FakePaymentProvider.Create: userId={UserId}, amount={Amount}, paymentId={PaymentId}",
            userId, amountRub, paymentId);

        return Task.FromResult(Result.Success<PaymentCreated, Error>(
            new PaymentCreated(paymentId, checkoutUrl)));
    }

    public Task<Result<PaymentVerification, Error>> VerifyWebhookAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken)
    {
        // Парсим только externalPaymentId — fake-формат
        // "{\"externalPaymentId\":\"fake-...\",\"amount\":49}".
        // Тесты сами строят payload через FakePayload.Build(...).
        string externalPaymentId;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            externalPaymentId = doc.RootElement.GetProperty("externalPaymentId").GetString()
                ?? string.Empty;
        }
        catch
        {
            return Task.FromResult(Result.Failure<PaymentVerification, Error>(
                Errors.Subscription.InvalidPaymentSignature()));
        }

        if (string.IsNullOrWhiteSpace(externalPaymentId))
        {
            return Task.FromResult(Result.Failure<PaymentVerification, Error>(
                Errors.Subscription.InvalidPaymentSignature()));
        }

        logger.LogInformation(
            "FakePaymentProvider.VerifyWebhook: paymentId={PaymentId} → Succeeded",
            externalPaymentId);

        return Task.FromResult(Result.Success<PaymentVerification, Error>(
            new PaymentVerification(externalPaymentId, PaymentStatus.Succeeded, 49m)));
    }

    public Task<UnitResult<Error>> CancelRecurringAsync(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "FakePaymentProvider.CancelRecurring: paymentId={PaymentId} (no-op)",
            externalPaymentId);
        return Task.FromResult(UnitResult.Success<Error>());
    }

    public Task<UnitResult<Error>> CancelPaymentAsync(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "FakePaymentProvider.CancelPayment: paymentId={PaymentId} → Success",
            externalPaymentId);
        return Task.FromResult(UnitResult.Success<Error>());
    }
}
