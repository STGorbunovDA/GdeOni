using GdeOni.Mobile.Services.Api;
using Refit;

namespace GdeOni.Mobile.Services.Media;

/// <summary>
/// D36. Реализация <see cref="IPublicHostsService"/>. Кеширует
/// <c>MediaBaseUrl</c> из <c>/api/app/features</c> на время сессии
/// процесса. Под капотом — SemaphoreSlim чтобы параллельные первые
/// вызовы (например, два item'а в списке одновременно вычисляют URL)
/// не дёргали /features больше одного раза.
/// </summary>
public sealed class PublicHostsService(IAppApi appApi) : IPublicHostsService
{
    /// <summary>
    /// DEBUG-дефолт для Android-эмулятора. 10.0.2.2 — это IP хост-машины
    /// внутри эмулятора. На реальном устройстве (USB-debug) этот URL
    /// не работает, но в этом сценарии бэк уже должен быть на доступном
    /// хосте и выдавать <see cref="Api.Models.AppFeaturesResponse.MediaBaseUrl"/>
    /// в /features. Release-сборка не использует этот фолбэк — она
    /// требует, чтобы бэк отдал реальный MediaBaseUrl.
    /// </summary>
    private const string DebugFallbackMediaBaseUrl = "http://10.0.2.2:9000";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedBaseUrl;
    private bool _isCached;

    public async Task<string?> BuildMediaUrlAsync(
        string? bucket,
        string? storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(storageKey))
            return null;

        var baseUrl = await GetMediaBaseUrlAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var trimmed = baseUrl.TrimEnd('/');
        var encodedKey = Uri.EscapeDataString(storageKey);
        return $"{trimmed}/{bucket}/{encodedKey}";
    }

    private async Task<string?> GetMediaBaseUrlAsync(CancellationToken cancellationToken)
    {
        if (_isCached)
            return _cachedBaseUrl;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_isCached)
                return _cachedBaseUrl;

            string? baseUrl = null;
            try
            {
                var envelope = await appApi.GetFeaturesAsync(cancellationToken);
                baseUrl = envelope.Result?.MediaBaseUrl;
            }
            catch (ApiException) { /* offline / 5xx — фолбэк ниже */ }
            catch (HttpRequestException) { /* нет сети */ }
            catch (TaskCanceledException) { /* timeout */ }

            // Если бэк не отдал MediaBaseUrl (старый бэк или сеть упала) —
            // в DEBUG используем 10.0.2.2:9000 (эмулятор). В Release фолбэка
            // нет: пусть фото не показывается, чем покажет битый URL.
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
#if DEBUG
                baseUrl = DebugFallbackMediaBaseUrl;
#else
                baseUrl = null;
#endif
            }

            _cachedBaseUrl = baseUrl;
            _isCached = true;
            return _cachedBaseUrl;
        }
        finally
        {
            _gate.Release();
        }
    }
}
