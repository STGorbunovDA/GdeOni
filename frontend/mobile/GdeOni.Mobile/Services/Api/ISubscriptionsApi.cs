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

    /// <summary>
    /// D16. Pull-fallback вместо webhook: бэк идёт к YooKassa за
    /// реальным статусом свежего Pending платежа и активирует
    /// подписку, если платёж succeeded. Дёргается перед каждым
    /// GetMy в SubscriptionViewModel, чтобы UI сразу видел актуальный
    /// статус даже если webhook не долетел (dev / сетевой сбой).
    /// 204 No Content, идемпотентно.
    /// </summary>
    [Post("/api/users/me/subscription/sync")]
    Task SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// D16. Юзер тапнул «Отменить оплату» на PendingPayment (нажал
    /// «Назад» на странице YooKassa): бэк закроет checkout, пометит
    /// платёж Cancelled и откатит подписку в Trial/Expired. 204.
    /// </summary>
    [Post("/api/users/me/subscription/pending/cancel")]
    Task CancelPendingAsync(CancellationToken cancellationToken = default);
}
