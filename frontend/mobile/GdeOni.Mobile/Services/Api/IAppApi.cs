using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// E22. App-level эндпоинты: версионность и feature flags.
/// </summary>
public interface IAppApi
{
    /// <summary>
    /// AllowAnonymous на бэке — клиент с протухшим токеном тоже
    /// должен узнать про force-update.
    /// </summary>
    [Get("/api/app/version")]
    Task<ApiEnvelope<AppVersionResponse>> GetVersionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Требует Authorize — флаги per-user могут отличаться (для
    /// админов SubscriptionEnabled фактически не применяется и т.п.).
    /// </summary>
    [Get("/api/app/features")]
    Task<ApiEnvelope<AppFeaturesResponse>> GetFeaturesAsync(
        CancellationToken cancellationToken = default);
}
