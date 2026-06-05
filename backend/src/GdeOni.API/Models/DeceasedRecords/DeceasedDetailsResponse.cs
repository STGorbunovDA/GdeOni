namespace GdeOni.API.Models.DeceasedRecords;

public sealed class DeceasedDetailsResponse
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? MiddleName { get; init; }
    public string FullName { get; init; } = null!;

    public DateOnly? BirthDate { get; init; }
    public DateOnly DeathDate { get; init; }

    public bool HasBurialLocation { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? AccuracyMeters { get; init; }
    public string? Country { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    // D11.12.1: enum-поле отдаётся строкой (как ModerationStatus,
    // RelationshipType, TrackStatus после D11.1.1). Раньше ехало int 1/2/3/4
    // и ломало симметрию с Create/Update-Request, где Accuracy уже строка.
    public string? Accuracy { get; init; }

    public string? ShortDescription { get; init; }
    public string? Biography { get; init; }

    public Guid CreatedByUserId { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    public DeceasedMetadataResponse Metadata { get; init; } = null!;
    public IReadOnlyCollection<DeceasedMemoryResponse> Memories { get; init; } = [];

    /// <summary>
    /// Id главного фото (если выбрано и Approved). Нужен для редактирования
    /// (например, в админке отметить какое сейчас главное).
    /// </summary>
    public Guid? MainMediaId { get; init; }

    /// <summary>
    /// Готовый публичный URL главного фото. Null если фото нет или оно
    /// не Approved. Зашит в ответ для устранения N+1 запросов на /media.
    /// </summary>
    public string? MainPhotoUrl { get; init; }
}
