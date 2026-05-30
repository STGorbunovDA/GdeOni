using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Model;

/// <summary>
/// D24. PATCH /api/deceased/{id}/burial-location — обновление полного
/// блока места захоронения трекающим юзером или админом.
///
/// Если все поля null/0 — Clear (sentinel: latitude null означает "удалить
/// координаты"). Альтернатива была бы отдельный DELETE-эндпоинт, но для
/// edit-формы на мобилке проще один PATCH.
/// </summary>
public sealed record UpdateBurialLocationByEditorCommand(
    Guid DeceasedId,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    LocationAccuracy Accuracy);
