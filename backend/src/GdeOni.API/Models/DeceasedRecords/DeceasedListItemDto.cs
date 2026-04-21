namespace GdeOni.API.Models.DeceasedRecords;

public class DeceasedListItemDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public DateTime? BirthDate { get; init; }
    public DateTime DeathDate { get; init; }
    public string Country { get; init; } = null!;
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}