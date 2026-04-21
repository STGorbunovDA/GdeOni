namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

public sealed class DeceasedDetailsResponse
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? MiddleName { get; init; }
    public string FullName { get; init; } = null!;

    public DateTime? BirthDate { get; init; }
    public DateTime DeathDate { get; init; }

    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Country { get; init; } = null!;
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? CemeteryName { get; init; }
    public string? PlotNumber { get; init; }
    public string? GraveNumber { get; init; }
    public int Accuracy { get; init; }

    public string? ShortDescription { get; init; }
    public string? Biography { get; init; }

    public Guid CreatedByUserId { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    public string SearchKey { get; init; } = null!;

    public DeceasedMetadataResponse Metadata { get; init; } = null!;
    public IReadOnlyCollection<DeceasedPhotoResponse> Photos { get; init; } = [];
    public IReadOnlyCollection<DeceasedMemoryResponse> Memories { get; init; } = [];
}