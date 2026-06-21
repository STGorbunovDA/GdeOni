using GdeOni.API.Models.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// D16. Мапперы DTO → command для <c>SubscriptionsController</c>.
/// </summary>
public static class SubscriptionsMapping
{
    /// <summary>Маппит DTO создания платежа в команду use case.</summary>
    public static CreatePaymentCommand ToCommand(this CreatePaymentRequest request) =>
        new(request.Plan);
}
