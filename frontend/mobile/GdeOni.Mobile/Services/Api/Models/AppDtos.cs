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
/// </summary>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry);
