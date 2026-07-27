using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Geo;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Geo;

/// <summary>
/// Прямое геокодирование через Nominatim (OpenStreetMap): текст адреса →
/// координаты. Зеркало <see cref="NominatimReverseGeocoder"/> (тот же HTTP-
/// клиент/настройки, кеш, мягкая обработка ошибок), только endpoint /search.
///
/// Ошибка (таймаут, 429, пустой ответ) — НЕ падение сценария: пользователь
/// поставит точку на карте руками. Поэтому логируем и возвращаем Failure.
/// </summary>
public sealed class NominatimForwardGeocoder(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<GeocodingOptions> options,
    ILogger<NominatimForwardGeocoder> logger) : IForwardGeocoder
{
    private readonly GeocodingOptions _options = options.Value;

    public async Task<Result<ForwardGeocodeResult, Error>> Search(
        string query,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return Errors.Geo.GeocodingUnavailable();

        var normalized = query.Trim();
        if (normalized.Length == 0)
            return Errors.Geo.AddressNotFound();

        var cacheKey = BuildCacheKey(normalized);
        if (cache.TryGetValue<ForwardGeocodeResult>(cacheKey, out var cached) && cached is not null)
            return Result.Success<ForwardGeocodeResult, Error>(cached);

        try
        {
            var url =
                $"/search?format=jsonv2&limit=1&accept-language=ru" +
                $"&q={Uri.EscapeDataString(normalized)}";

            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nominatim /search вернул {StatusCode} для «{Query}»",
                    (int)response.StatusCode, normalized);
                return Errors.Geo.GeocodingUnavailable();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = Parse(json);

            if (parsed is null)
                return Errors.Geo.AddressNotFound();

            cache.Set(cacheKey, parsed, TimeSpan.FromHours(_options.CacheHours));
            return Result.Success<ForwardGeocodeResult, Error>(parsed);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Nominatim /search не ответил за {Timeout}с", _options.TimeoutSeconds);
            return Errors.Geo.GeocodingUnavailable();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Сетевая ошибка при обращении к Nominatim /search");
            return Errors.Geo.GeocodingUnavailable();
        }
    }

    /// <summary>Берёт первый результат: lat/lon (строками) + display_name.</summary>
    private static ForwardGeocodeResult? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        var first = root[0];
        var latStr = GetString(first, "lat");
        var lonStr = GetString(first, "lon");

        if (latStr is null || lonStr is null)
            return null;

        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return null;

        return new ForwardGeocodeResult(lat, lon, GetString(first, "display_name"));
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;

    private static string BuildCacheKey(string query) =>
        $"geo:search:{query.ToLowerInvariant()}";
}
