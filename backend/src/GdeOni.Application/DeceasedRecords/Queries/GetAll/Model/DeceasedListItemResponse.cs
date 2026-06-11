namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

public sealed class GetAllDeceasedItemResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }
    public bool HasBurialLocation { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? AccuracyMeters { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Id главного фото (Approved). Нужен клиенту чтобы потом, если
    /// он откроет редактор, знать какое сейчас выбрано.
    /// </summary>
    public Guid? MainMediaId { get; init; }

    /// <summary>
    /// Готовый публичный URL главного фото для превью в ленте.
    /// Null если фото нет или оно не Approved. Лекарство от N+1.
    /// </summary>
    public string? MainPhotoUrl { get; init; }
}