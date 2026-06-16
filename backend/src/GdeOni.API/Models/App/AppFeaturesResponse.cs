namespace GdeOni.API.Models.App;

/// <summary>
/// Ответ <c>GET /api/app/features</c>. Решение 2026-05-14: per-feature
/// gating отсутствует — подписка единая на всё приложение.
///
/// D36 (2026-06-12): добавлен <see cref="MediaBaseUrl"/>. Клиенты сами
/// строят media-URL через <c>${MediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>,
/// потому что хост MinIO/CDN различается для web (localhost), Android-
/// эмулятора (10.0.2.2) и production (CDN-домен).
/// </summary>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry,
    string MediaBaseUrl);
