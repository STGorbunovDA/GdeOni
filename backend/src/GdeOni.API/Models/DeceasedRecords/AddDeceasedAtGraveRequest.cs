using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

public sealed class AddDeceasedAtGraveRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateOnly DeathDate { get; set; }
    public string? ShortDescription { get; set; }
    public string? Biography { get; set; }
    public AddDeceasedAtGraveLocationRequest GraveLocation { get; set; } = null!;
    public AddDeceasedAtGraveTrackingRequest Tracking { get; set; } = null!;
}

public sealed class AddDeceasedAtGraveLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? CemeteryName { get; set; }
    public string? PlotNumber { get; set; }
    public string? GraveNumber { get; set; }
}

public sealed class AddDeceasedAtGraveTrackingRequest
{
    public RelationshipType RelationshipType { get; set; }
    public string? PersonalNotes { get; set; }
    public bool NotifyOnDeathAnniversary { get; set; }
    public bool NotifyOnBirthAnniversary { get; set; }
}
