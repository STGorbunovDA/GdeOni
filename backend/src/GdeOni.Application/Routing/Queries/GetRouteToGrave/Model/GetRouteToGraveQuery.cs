using GdeOni.Domain.Shared;

namespace GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;

public sealed record GetRouteToGraveQuery(
    Guid DeceasedId,
    double FromLat,
    double FromLon,
    RoutingMode Mode);
