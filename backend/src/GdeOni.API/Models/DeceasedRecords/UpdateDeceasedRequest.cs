using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

public sealed class UpdateDeceasedRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }

    public DateTime? BirthDate { get; set; }
    public DateTime DeathDate { get; set; }

    public string? ShortDescription { get; set; }
    public string? Biography { get; set; }

    public UpdateDeceasedBurialLocationRequest? BurialLocation { get; set; }
    public UpdateDeceasedMetadataRequest? Metadata { get; set; }
}

public sealed class UpdateDeceasedBurialLocationRequest
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

public sealed class UpdateDeceasedMetadataRequest
{
    public string? Epitaph { get; set; }
    public string? Religion { get; set; }
    public string? Source { get; set; }
    public bool IsMilitaryService { get; set; }
    public string? AdditionalInfo { get; set; }
}