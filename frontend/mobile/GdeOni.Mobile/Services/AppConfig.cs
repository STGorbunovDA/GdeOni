using System.Text.Json;
using System.Text.Json.Serialization;

namespace GdeOni.Mobile.Services;

public sealed record AppConfig(
    [property: JsonPropertyName("Api")] ApiConfig Api,
    [property: JsonPropertyName("Environment")] string Environment,
    [property: JsonPropertyName("Sentry")] SentryConfig? Sentry = null);

public sealed record ApiConfig(
    [property: JsonPropertyName("BaseUrl")] string BaseUrl,
    [property: JsonPropertyName("TimeoutSeconds")] int TimeoutSeconds);

/// <summary>
/// E25. Mobile Sentry config. Если Dsn null/пустой — Sentry no-op.
/// TracesSampleRate=0 в проде (мобильным performance-traces не нужны
/// до отдельного решения по бюджету Sentry).
/// </summary>
public sealed record SentryConfig(
    [property: JsonPropertyName("Dsn")] string? Dsn,
    [property: JsonPropertyName("Environment")] string? Environment = null,
    [property: JsonPropertyName("Release")] string? Release = null,
    [property: JsonPropertyName("TracesSampleRate")] double TracesSampleRate = 0.0);

public static class AppConfigLoader
{
    private const string FileName = "appsettings.json";

    /// <summary>
    /// Синхронная загрузка config через MAUI FileSystem. Под капотом
    /// FileSystem.OpenAppPackageFileAsync на Android — это обёртка над
    /// AssetManager.open (синхронная операция), поэтому GetResult безопасен.
    /// </summary>
    public static AppConfig Load()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(FileName).GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<AppConfig>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize {FileName}.");
    }
}
