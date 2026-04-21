namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;

public sealed record GetDistanceResponse(
    Guid DeceasedId,
    double Latitude,
    double Longitude,
    double DistanceKm);