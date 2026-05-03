namespace GdeOni.API.Models.DeceasedRecords;

public sealed class SetBurialLocationFromGpsRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
}
