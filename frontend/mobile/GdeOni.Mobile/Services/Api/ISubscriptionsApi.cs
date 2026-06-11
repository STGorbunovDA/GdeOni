using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// E22. Платная подписка через YooKassa. CheckoutUrl открывается
/// внешним браузером, после оплаты юзер вручную возвращается в
/// приложение и pull-to-refresh-ит SubscriptionPage.
/// </summary>
public interface ISubscriptionsApi
{
    [Get("/api/users/me/subscription")]
    Task<ApiEnvelope<MySubscriptionResponse>> GetMyAsync(
        CancellationToken cancellationToken = default);

    [Post("/api/users/me/subscription/create-payment")]
    Task<ApiEnvelope<CreatePaymentResponse>> CreatePaymentAsync(
        [Body] CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/users/me/subscription/cancel")]
    Task CancelAsync(CancellationToken cancellationToken = default);
}
