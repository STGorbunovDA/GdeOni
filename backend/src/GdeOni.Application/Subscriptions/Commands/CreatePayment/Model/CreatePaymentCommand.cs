using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;

/// <summary>
/// D16. Команда создания платежа. UserId не нужен — берётся из JWT.
/// </summary>
public sealed record CreatePaymentCommand(SubscriptionPlan Plan);
