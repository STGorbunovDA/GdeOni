using GdeOni.API.Models.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Mappers;

/// <summary>
/// D16. Мапперы DTO → command для <c>SubscriptionsController</c>.
/// </summary>
public static class SubscriptionsMapping
{
    /// <summary>
    /// Маппит DTO создания платежа в команду use case. null-Platform
    /// (старый mobile-клиент, не знающий про поле) трактуется как
    /// <see cref="ClientPlatform.Mobile"/> — deep-link возвращает
    /// юзера в приложение.
    /// </summary>
    public static CreatePaymentCommand ToCommand(this CreatePaymentRequest request) =>
        new(request.Plan, request.Platform ?? ClientPlatform.Mobile);
}
