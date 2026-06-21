namespace GdeOni.API.Models.Routing;

/// <summary>
/// Ответ построения маршрута до места захоронения: исходные точки и
/// ссылки в внешние навигаторы.
/// </summary>
public sealed class RouteResponse
{
    /// <summary>Идентификатор карточки умершего.</summary>
    public Guid DeceasedId { get; init; }
    /// <summary>Координаты могилы (конечная точка маршрута).</summary>
    public GraveLocationResponse GraveLocation { get; init; } = null!;
    /// <summary>Координаты пользователя (стартовая точка маршрута).</summary>
    public FromCoordinatesResponse From { get; init; } = null!;
    /// <summary>Режим маршрутизации (driving/walking/transit).</summary>
    public string Mode { get; init; } = null!;
    /// <summary>Список deep-link'ов в поддерживаемые навигаторы.</summary>
    public IReadOnlyCollection<RouteLinkResponse> Links { get; init; } = [];
}

/// <summary>
/// Координаты места захоронения (конечная точка маршрута).
/// </summary>
public sealed class GraveLocationResponse
{
    /// <summary>Широта.</summary>
    public double Latitude { get; init; }
    /// <summary>Долгота.</summary>
    public double Longitude { get; init; }
    /// <summary>Заявленная точность GPS в метрах.</summary>
    public double? AccuracyMeters { get; init; }
}

/// <summary>
/// Координаты исходной точки (текущая позиция пользователя).
/// </summary>
public sealed class FromCoordinatesResponse
{
    /// <summary>Широта.</summary>
    public double Latitude { get; init; }
    /// <summary>Долгота.</summary>
    public double Longitude { get; init; }
}

/// <summary>
/// Deep-link в конкретный внешний навигатор (Google Maps, Yandex Maps и т.д.).
/// </summary>
public sealed class RouteLinkResponse
{
    /// <summary>Имя провайдера навигатора.</summary>
    public string Provider { get; init; } = null!;
    /// <summary>URL для запуска навигатора с готовым маршрутом.</summary>
    public string Url { get; init; } = null!;
}
