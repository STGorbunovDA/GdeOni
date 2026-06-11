using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Shared.Versioning;
using Refit;

namespace GdeOni.Mobile.Services.Versioning;

public sealed class AppVersionCheckService(IAppApi appApi) : IAppVersionCheckService
{
    public async Task<VersionCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var envelope = await appApi.GetVersionAsync(cancellationToken);
            var data = envelope.Result;
            if (data is null)
                return VersionCheckResult.Ok();

            return AppVersionGate.Evaluate(
                currentVersion: AppInfo.Current.VersionString,
                minSupportedVersion: data.MinSupportedVersion,
                latestVersion: data.LatestVersion,
                downloadUrl: data.DownloadUrl,
                forceUpdateMessage: data.ForceUpdateMessage);
        }
        catch (ApiException)
        {
            // Бэк ответил, но не 2xx (например, 500). Не блокируем юзера —
            // он мог запустить приложение в момент рестарта сервера.
            return VersionCheckResult.Ok();
        }
        catch (HttpRequestException)
        {
            // Нет сети — fail-open.
            return VersionCheckResult.Ok();
        }
        catch (TaskCanceledException)
        {
            // Таймаут — fail-open.
            return VersionCheckResult.Ok();
        }
    }
}
