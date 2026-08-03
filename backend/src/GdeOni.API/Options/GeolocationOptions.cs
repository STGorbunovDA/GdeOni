namespace GdeOni.API.Options;

/// <summary>
/// Клиентские настройки геолокации (веб). Окно сбора «лучшего» GPS-фикса:
/// сколько секунд собирать замеры перед тем, как взять самый точный. Значение
/// отдаётся клиенту через <c>GET /api/app/features</c>, поэтому меняется без
/// пересборки фронта — правишь appsettings.json → рестарт API.
/// Биндится из секции <c>Geolocation</c>.
/// </summary>
public sealed class GeolocationOptions
{
    /// <summary>Имя секции в appsettings.</summary>
    public const string SectionName = "Geolocation";

    /// <summary>
    /// Окно сбора координат, секунды. Дефолт 60. На клиент отдаётся с
    /// клампом в разумные границы (см. AppController).
    /// </summary>
    public int AcquireWindowSeconds { get; set; } = 60;
}
