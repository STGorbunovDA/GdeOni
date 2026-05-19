namespace GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;

/// <summary>
/// Карточка умершего из результата поиска "рядом". В отличие от
/// GetAllDeceasedItemResponse тут всегда есть координаты + расстояние
/// до точки поиска в метрах (отсортировано по возрастанию).
/// </summary>
public sealed class NearbyDeceasedItemResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    public bool IsVerified { get; init; }

    /// <summary>
    /// Расстояние от точки поиска (Query.Latitude/Longitude) до могилы,
    /// в метрах. Округлено к ближайшему целому. Для UI: "120 м" или
    /// "1.2 км" (форматирует клиент).
    /// </summary>
    public int DistanceMeters { get; init; }
}
