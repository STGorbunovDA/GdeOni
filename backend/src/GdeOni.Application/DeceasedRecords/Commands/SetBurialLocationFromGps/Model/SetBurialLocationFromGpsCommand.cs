namespace GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;

public sealed record SetBurialLocationFromGpsCommand(
    Guid DeceasedId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters);
