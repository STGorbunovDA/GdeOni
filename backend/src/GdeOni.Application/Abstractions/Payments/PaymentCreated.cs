namespace GdeOni.Application.Abstractions.Payments;

/// <summary>
/// D16. Результат создания платежа в <see cref="IPaymentProvider.CreateAsync"/>.
/// </summary>
/// <param name="ExternalPaymentId">
/// ID платежа в системе провайдера (YooKassa). Сохраняется в
/// <c>Subscription.LastPaymentId</c> — по нему позже находим юзера
/// при обработке webhook.
/// </param>
/// <param name="CheckoutUrl">
/// URL платёжной страницы. Клиент (mobile/web) открывает его в
/// браузере / WebView для оплаты.
/// </param>
public sealed record PaymentCreated(
    string ExternalPaymentId,
    string CheckoutUrl);
