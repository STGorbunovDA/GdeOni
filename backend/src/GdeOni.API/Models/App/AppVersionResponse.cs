namespace GdeOni.API.Models.App;

/// <summary>
/// Ответ <c>GET /api/app/version</c>. Mobile/web дёргает при каждом
/// старте; если currentVersion &lt; MinSupportedVersion — blocking
/// update screen.
/// </summary>
public sealed record AppVersionResponse(
    string MinSupportedVersion,
    string LatestVersion,
    string? ForceUpdateMessage,
    string? DownloadUrl);
