using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Routing;

/// <summary>
/// Провайдер deep-link'а на построение маршрута во внешней карте.
/// Backend сам маршрут не строит — отдаёт URL, который мобильный
/// клиент или браузер открывают в Яндекс.Картах / Google Maps / 2GIS.
/// </summary>
public interface IRouteLinkProvider
{
    /// <summary>
    /// Уникальный ключ провайдера (например, "yandex", "google", "2gis").
    /// Попадает в response, чтобы клиент мог сматчить ссылку с иконкой.
    /// </summary>
    string ProviderKey { get; }

    /// <summary>
    /// Построить deep-link маршрута от (fromLat, fromLon) до (toLat, toLon)
    /// в указанном режиме. Реализации не делают сетевых вызовов — только
    /// форматируют URL.
    /// </summary>
    string BuildLink(double fromLat, double fromLon, double toLat, double toLon, RoutingMode mode);
}
