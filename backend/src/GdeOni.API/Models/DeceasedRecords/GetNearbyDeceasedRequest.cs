namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// E21. Параметры поиска "кто рядом" — координаты пользователя и
/// радиус. Запросом владеет авторизованный юзер (на кладбище ищет
/// умершего, чтобы добавить в отслеживание и построить маршрут).
/// </summary>
public sealed class GetNearbyDeceasedRequest
{
    /// <summary>Широта GPS-координаты пользователя.</summary>
    public double Latitude { get; set; }
    /// <summary>Долгота GPS-координаты пользователя.</summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Радиус в метрах. Default 100м — типичный сценарий
    /// "стою на участке кладбища". Допустимый диапазон валидируется
    /// в GetNearbyDeceasedQueryValidator [10, 5000] м.
    /// </summary>
    public double RadiusMeters { get; set; } = 100;

    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
}
