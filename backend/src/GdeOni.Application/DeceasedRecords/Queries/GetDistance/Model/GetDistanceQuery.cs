namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;

public sealed record GetDistanceQuery(
    Guid DeceasedId,
    double Latitude,
    double Longitude);