namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;

/// <summary>
/// D16. Ответ: URL на платёжную страницу YooKassa.
/// </summary>
public sealed record CreatePaymentResponse(
    string CheckoutUrl,
    string ExternalPaymentId);
