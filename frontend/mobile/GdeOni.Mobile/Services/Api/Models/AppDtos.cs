namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// E22. <c>GET /api/app/version</c>. AllowAnonymous на бэке —
/// мобилка дёргает на старте до логина, чтобы поймать force-update
/// даже для протухшего токена.
/// </summary>
public sealed record AppVersionResponse(
    string MinSupportedVersion,
    string LatestVersion,
    string? ForceUpdateMessage,
    string? DownloadUrl);

/// <summary>
/// E22. <c>GET /api/app/features</c>. Кешируется на сессию —
/// определяет, показывать ли paywall и какие фичи доступны.
///
/// D36 (2026-06-12): добавлен <see cref="MediaBaseUrl"/>. Mobile строит
/// URL фото через <c>${MediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>.
/// Если бэк ещё старый (нет поля) — клиент использует дефолт
/// <c>http://10.0.2.2:9000</c> для DEBUG (Android-эмулятор) или
/// production-домен в Release.
/// </summary>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry,
    string? MediaBaseUrl = null);
