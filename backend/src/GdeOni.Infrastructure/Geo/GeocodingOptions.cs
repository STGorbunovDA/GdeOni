namespace GdeOni.Infrastructure.Geo;

/// <summary>
/// D41. Настройки обратного геокодирования. Секция <c>Geocoding</c>.
/// </summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>
    /// Выключатель. Если false — API отвечает «не определилось», клиент
    /// молча оставляет поля пустыми. Полезно, если Nominatim забанит нас
    /// или мы упрёмся в лимиты: сценарий добавления карточки не должен
    /// падать из-за необязательной подсказки.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Базовый URL геокодера.</summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// User-Agent. Nominatim ТРЕБУЕТ осмысленный UA с контактом, иначе
    /// блокирует по IP (их Usage Policy). Без него сервис работать не будет.
    /// </summary>
    public string UserAgent { get; set; } = "GdeOni/1.0 (support@gdeoni.ru)";

    /// <summary>
    /// Таймаут. Короткий намеренно: геокодинг — подсказка, а не блокирующий
    /// шаг. Лучше не подставить город, чем заставить юзера ждать у могилы.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Сколько часов держать ответ в кеше. Кладбища не переезжают, а
    /// Nominatim ограничивает 1 запрос в секунду — кеш снимает большую
    /// часть нагрузки (несколько человек у одной могилы = один запрос).
    /// </summary>
    public int CacheHours { get; set; } = 720;

    /// <summary>
    /// До скольких знаков округлять координаты для ключа кеша.
    /// 4 знака ≈ 11 метров — в пределах одного кладбища попадания в кеш
    /// будут частыми, а город на таком расстоянии не меняется.
    /// </summary>
    public int CachePrecision { get; set; } = 4;
}
