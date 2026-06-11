namespace GdeOni.API.Models.Routing;

public sealed class RouteResponse
{
    public Guid DeceasedId { get; init; }
    public GraveLocationResponse GraveLocation { get; init; } = null!;
    public FromCoordinatesResponse From { get; init; } = null!;
    public string Mode { get; init; } = null!;
    public IReadOnlyCollection<RouteLinkResponse> Links { get; init; } = [];
}

public sealed class GraveLocationResponse
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? AccuracyMeters { get; init; }
}

public sealed class FromCoordinatesResponse
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public sealed class RouteLinkResponse
{
    public string Provider { get; init; } = null!;
    public string Url { get; init; } = null!;
}
