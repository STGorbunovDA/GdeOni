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
/// D41. Обратное геокодирование через Nominatim (OpenStreetMap).
///
/// Почему Nominatim: бесплатно и без ключа — на старте этого достаточно.
/// Ограничения (1 req/sec, обязательный User-Agent) мы гасим кешем и
/// коротким таймаутом. Если упрёмся в лимиты или качество по РФ — меняем
/// реализацию <see cref="IReverseGeocoder"/> на Яндекс.Геокодер, не трогая
/// ни use case, ни клиентов.
///
/// Любая ошибка (таймаут, 429, мусор в ответе) — это НЕ падение сценария:
/// геокодинг лишь подсказывает город, юзер всегда может вписать его сам.
/// Поэтому ошибки логируем и возвращаем как обычный Failure, а не
/// исключение.
/// </summary>
public sealed class NominatimReverseGeocoder(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<GeocodingOptions> options,
    ILogger<NominatimReverseGeocoder> logger) : IReverseGeocoder
{
    private readonly GeocodingOptions _options = options.Value;

    public static void ConfigureClient(HttpClient http, GeocodingOptions opts)
    {
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        // Nominatim блокирует запросы без осмысленного User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
    }

    public async Task<Result<ReverseGeocodeResult, Error>> Reverse(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return Errors.Geo.GeocodingUnavailable();

        var cacheKey = BuildCacheKey(latitude, longitude);
        if (cache.TryGetValue<ReverseGeocodeResult>(cacheKey, out var cached) && cached is not null)
            return Result.Success<ReverseGeocodeResult, Error>(cached);

        try
        {
            // zoom=14 — уровень «город/район»: не приносит номер дома,
            // который нам не нужен, и стабильнее на пустырях и кладбищах.
            var url =
                $"/reverse?format=jsonv2&zoom=14&addressdetails=1" +
                $"&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&accept-language=ru";

            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nominatim вернул {StatusCode} для ({Lat}, {Lon})",
                    (int)response.StatusCode, latitude, longitude);
                return Errors.Geo.GeocodingUnavailable();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = Parse(json);

            if (parsed is null)
                return Errors.Geo.AddressNotFound();

            cache.Set(cacheKey, parsed, TimeSpan.FromHours(_options.CacheHours));
            return Result.Success<ReverseGeocodeResult, Error>(parsed);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Таймаут геокодера. Юзер стоит у могилы — ждать он не должен.
            logger.LogWarning("Nominatim не ответил за {Timeout}с", _options.TimeoutSeconds);
            return Errors.Geo.GeocodingUnavailable();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Сетевая ошибка при обращении к Nominatim");
            return Errors.Geo.GeocodingUnavailable();
        }
    }

    /// <summary>
    /// Достаёт страну / регион / город из блока address.
    ///
    /// Города в OSM размечены по-разному в зависимости от размера
    /// населённого пункта: city → town → village → municipality. Берём
    /// первое непустое, иначе на деревенском кладбище город всегда был бы
    /// пустым.
    /// </summary>
    private static ReverseGeocodeResult? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("address", out var address))
            return null;

        var country = GetString(address, "country");
        var region = GetString(address, "state")
                     ?? GetString(address, "region");

        // Порядок важен: сначала сам населённый пункт (city/town/village/
        // hamlet), и только если его нет — административная единица. Иначе
        // на деревенском кладбище город остался бы пустым.
        var city = GetString(address, "city")
                   ?? GetString(address, "town")
                   ?? GetString(address, "village")
                   ?? GetString(address, "hamlet")
                   ?? GetString(address, "settlement")
                   ?? GetString(address, "municipality")
                   ?? GetString(address, "county");

        city = NormalizeCity(city);

        if (country is null && region is null && city is null)
            return null;

        return new ReverseGeocodeResult(country, region, city);
    }

    /// <summary>
    /// Срезает у города название административной единицы.
    ///
    /// OSM для части городов кладёт в поле <c>city</c> не «Тверь», а
    /// «городской округ Тверь» — так размечены полигоны. В карточке
    /// умершего это выглядит нелепо, а поиск по городу такую строку не
    /// найдёт. Москва и Питер приходят чистыми, Тверь и Химки — нет,
    /// поэтому чистим все.
    /// </summary>
    private static string? NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;

        string[] prefixes =
        [
            "городской округ ",
            "муниципальный округ ",
            "муниципальный район ",
            "городское поселение ",
            "сельское поселение ",
            "городской посёлок ",
            "рабочий посёлок ",
        ];

        var result = city.Trim();
        foreach (var prefix in prefixes)
        {
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result[prefix.Length..].Trim();
                break;
            }
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;

    /// <summary>
    /// Ключ кеша — округлённые координаты. Соседние могилы на одном
    /// кладбище схлопываются в один ключ: город у них всё равно один.
    /// </summary>
    private string BuildCacheKey(double latitude, double longitude)
    {
        var lat = Math.Round(latitude, _options.CachePrecision);
        var lon = Math.Round(longitude, _options.CachePrecision);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"geo:reverse:{lat}:{lon}");
    }
}
