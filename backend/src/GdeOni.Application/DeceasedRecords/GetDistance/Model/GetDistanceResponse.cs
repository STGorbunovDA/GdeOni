namespace GdeOni.Application.DeceasedRecords.GetDistance.Model;

public sealed record GetDistanceResponse(
    Guid DeceasedId,
    double Latitude,
    double Longitude,
    double DistanceKm);