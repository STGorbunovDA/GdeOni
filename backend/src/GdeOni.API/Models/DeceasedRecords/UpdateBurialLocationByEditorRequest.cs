using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос правки места захоронения от лица редактора (админа/модератора).
/// Все поля nullable: null означает "не менять текущее значение".
/// </summary>
/// <param name="Latitude">Новая широта.</param>
/// <param name="Longitude">Новая долгота.</param>
/// <param name="AccuracyMeters">Заявленная точность GPS в метрах.</param>
/// <param name="Country">Страна.</param>
/// <param name="Region">Регион/область.</param>
/// <param name="City">Город или населённый пункт.</param>
/// <param name="CemeteryName">Название кладбища.</param>
/// <param name="PlotNumber">Номер участка/сектора.</param>
/// <param name="GraveNumber">Номер могилы.</param>
/// <param name="Accuracy">Уровень точности координат (Exact/Approximate и т.д.).</param>
public sealed record UpdateBurialLocationByEditorRequest(
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    LocationAccuracy Accuracy = LocationAccuracy.Exact);
