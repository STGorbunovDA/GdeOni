using GdeOni.API.Models.Routing;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Маппинг Application result-DTO (GetRouteToGraveResult) →
/// presentation Response (RouteResponse). Живёт в API: задача
/// presentation-слоя, аналог D7.62.
/// </summary>
public static class RoutingResponseMapping
{
    public static RouteResponse ToRouteResponse(this GetRouteToGraveResult result) =>
        new()
        {
            DeceasedId = result.DeceasedId,
            GraveLocation = new GraveLocationResponse
            {
                Latitude = result.GraveLat,
                Longitude = result.GraveLon,
                AccuracyMeters = result.AccuracyMeters
            },
            From = new FromCoordinatesResponse
            {
                Latitude = result.FromLat,
                Longitude = result.FromLon
            },
            Mode = result.Mode.ToString(),
            Links = result.Links
                .Select(link => new RouteLinkResponse
                {
                    Provider = link.Provider,
                    Url = link.Url
                })
                .ToArray()
        };
}
