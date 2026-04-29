using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

public sealed class CreateDeceasedRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }
    public DateOnly DeathDate { get; set; }

    public string? ShortDescription { get; set; }
    public string? Biography { get; set; }

    public CreateDeceasedBurialLocationRequest? BurialLocation { get; set; }
    public IReadOnlyCollection<CreateDeceasedPhotoRequest>? Photos { get; set; }
    public IReadOnlyCollection<CreateDeceasedMemoryRequest>? Memories { get; set; }
    public CreateDeceasedMetadataRequest? Metadata { get; set; }
}

public sealed class CreateDeceasedBurialLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }

    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? CemeteryName { get; set; }
    public string? PlotNumber { get; set; }
    public string? GraveNumber { get; set; }

    public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.Exact;
}

public sealed class CreateDeceasedPhotoRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class CreateDeceasedMemoryRequest
{
    public string Text { get; set; } = string.Empty;
}

public sealed class CreateDeceasedMetadataRequest
{
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}