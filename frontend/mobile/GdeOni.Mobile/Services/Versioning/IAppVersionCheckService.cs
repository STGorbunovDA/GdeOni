using GdeOni.Mobile.Shared.Versioning;

namespace GdeOni.Mobile.Services.Versioning;

/// <summary>
/// E22. Обёртка над <c>GET /api/app/version</c> + <c>AppInfo.Current</c>.
/// На старте приложения вызывается один раз; результат используется
/// для решения, идти на blocking-update или продолжать.
/// </summary>
public interface IAppVersionCheckService
{
    /// <summary>
    /// Возвращает результат проверки. При сетевой ошибке /
    /// невалидном ответе — <see cref="VersionCheckResult.Ok"/>
    /// (fail-open: не блокируем юзера если бэк недоступен).
    /// </summary>
    Task<VersionCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
