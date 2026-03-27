using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Update.Model;

public sealed class UpdateDeceasedBurialLocationDto
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