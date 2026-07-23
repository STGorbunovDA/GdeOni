namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос установки места захоронения карточки умершего из текущей
/// GPS-позиции пользователя.
/// </summary>
public sealed class SetBurialLocationFromGpsRequest
{
    /// <summary>Широта GPS-координаты пользователя.</summary>
    public double Latitude { get; set; }
    /// <summary>Долгота GPS-координаты пользователя.</summary>
    public double Longitude { get; set; }
    /// <summary>Заявленная погрешность GPS в метрах (если известна).</summary>
    public double? AccuracyMeters { get; set; }
}
