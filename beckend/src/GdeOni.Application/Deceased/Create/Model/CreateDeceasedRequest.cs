using GdeOni.Domain.Shared;

namespace GdeOni.Application.Deceased.Create.Model;

public sealed class CreateDeceasedRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }

    public DateTime? BirthDate { get; set; }
    public DateTime DeathDate { get; set; }

    public string? ShortDescription { get; set; }
    public string? Biography { get; set; }

    public Guid CreatedByUserId { get; set; }

    public CreateDeceasedBurialLocationDto BurialLocation { get; set; } = null!;

    public List<CreateDeceasedPhotoDto>? Photos { get; set; }
    public List<CreateDeceasedMemoryDto>? Memories { get; set; }
    public CreateDeceasedMetadataDto? Metadata { get; set; }
}

public sealed class CreateDeceasedBurialLocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Country { get; set; } = null!;
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? CemeteryName { get; set; }
    public string? PlotNumber { get; set; }
    public string? GraveNumber { get; set; }

    public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.Exact;
}

public sealed class CreateDeceasedPhotoDto
{
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
    public Guid AddedByUserId { get; set; }
}

public sealed class CreateDeceasedMemoryDto
{
    public string Text { get; set; } = null!;
    public string? AuthorDisplayName { get; set; }
    public Guid? AuthorUserId { get; set; }
}

public sealed class CreateDeceasedMetadataDto
{
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}