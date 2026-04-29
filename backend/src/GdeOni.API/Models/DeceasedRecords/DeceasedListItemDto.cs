namespace GdeOni.API.Models.DeceasedRecords;

public class DeceasedListItemDto
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
}