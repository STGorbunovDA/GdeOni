using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Subscriptions;

/// <summary>
/// D16. Тело <c>POST /api/users/me/subscription/create-payment</c>.
/// Plan приходит строкой (`"Monthly"`) благодаря
/// <c>JsonStringEnumConverter</c> в Program.cs.
/// </summary>
public sealed record CreatePaymentRequest(SubscriptionPlan Plan);
