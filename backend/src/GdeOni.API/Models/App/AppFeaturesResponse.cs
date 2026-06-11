namespace GdeOni.API.Models.App;

/// <summary>
/// Ответ <c>GET /api/app/features</c>. Решение 2026-05-14: per-feature
/// gating отсутствует — подписка единая на всё приложение.
/// </summary>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry);
