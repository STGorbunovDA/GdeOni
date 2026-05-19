using System.Text.Json;
using System.Text.Json.Serialization;

namespace GdeOni.Mobile.Services;

public sealed record AppConfig(
    [property: JsonPropertyName("Api")] ApiConfig Api,
    [property: JsonPropertyName("Environment")] string Environment);

public sealed record ApiConfig(
    [property: JsonPropertyName("BaseUrl")] string BaseUrl,
    [property: JsonPropertyName("TimeoutSeconds")] int TimeoutSeconds);

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
