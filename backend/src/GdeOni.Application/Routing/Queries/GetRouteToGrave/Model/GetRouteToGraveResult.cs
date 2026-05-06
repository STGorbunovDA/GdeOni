using GdeOni.Domain.Shared;

namespace GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;

public sealed record GetRouteToGraveResult(
    Guid DeceasedId,
    double GraveLat,
    double GraveLon,
    double? AccuracyMeters,
    double FromLat,
    double FromLon,
    RoutingMode Mode,
    IReadOnlyCollection<RouteLink> Links);

public sealed record RouteLink(string Provider, string Url);
