namespace GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;

/// <summary>
/// D16. Команда обработки webhook от платёжного провайдера.
/// Аноним — auth обеспечивается HMAC-подписью внутри payload
/// (см. <see cref="GdeOni.Application.Abstractions.Payments.IPaymentProvider.VerifyWebhookAsync"/>).
/// </summary>
/// <param name="Payload">Сырое тело запроса (для HMAC verify).</param>
/// <param name="SignatureHeader">
/// HTTP-заголовок с подписью (название зависит от провайдера).
/// Может быть null — провайдер сам решает, обязателен ли он.
/// </param>
public sealed record ProcessPaymentWebhookCommand(
    string Payload,
    string? SignatureHeader);
