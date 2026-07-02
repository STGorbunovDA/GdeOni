using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;

/// <summary>
/// D16. Команда создания платежа. UserId не нужен — берётся из JWT.
/// <paramref name="Platform"/> определяет URL возврата (deep-link для
/// mobile, /payment/return для web). Старые mobile-клиенты, которые
/// не передают поле, десериализуются в <see cref="ClientPlatform.Mobile"/>
/// (default enum) — это дефолт для обратной совместимости.
/// </summary>
public sealed record CreatePaymentCommand(
    SubscriptionPlan Plan,
    ClientPlatform Platform);
