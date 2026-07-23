using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Subscriptions;

/// <summary>
/// D16. Тело <c>POST /api/users/me/subscription/create-payment</c>.
/// <see cref="Plan"/> и <see cref="Platform"/> приходят строками
/// ("Monthly" / "Web" / "Mobile") благодаря <c>JsonStringEnumConverter</c>
/// в Program.cs. <see cref="Platform"/> null для обратной совместимости
/// со старыми mobile-клиентами — маппер подставит <c>Mobile</c>.
/// </summary>
public sealed record CreatePaymentRequest(
    SubscriptionPlan Plan,
    ClientPlatform? Platform);
