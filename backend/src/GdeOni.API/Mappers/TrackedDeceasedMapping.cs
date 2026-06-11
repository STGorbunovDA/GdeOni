using GdeOni.API.Models.Routing;
using GdeOni.API.Models.Users;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Mappers;

public static class TrackedDeceasedMapping
{
    public static GetMyTrackedDeceasedListQuery ToQuery(this GetMyTrackedDeceasedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetMyTrackedDeceasedListQuery(request.Page, request.PageSize);
    }

    public static GetMyTrackedDeceasedDetailsQuery ToDetailsQuery(Guid deceasedId)
        => new(deceasedId);

    public static IsTrackedByMeQuery ToIsTrackedByMeQuery(Guid deceasedId)
        => new(deceasedId);

    public static TrackDeceasedCommand ToCommand(this AddMeTrackingRequest request, Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new TrackDeceasedCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);
    }

    public static UpdateTrackingCommand ToCommand(this UpdateTrackingRequest request, Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateTrackingCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary,
            request.TrackStatus);
    }

    public static RemoveTrackingCommand ToRemoveCommand(Guid deceasedId)
        => new(deceasedId);

    public static GetRouteToGraveQuery ToRouteQuery(
        Guid deceasedId,
        double fromLat,
        double fromLon,
        RoutingMode mode)
        => new(deceasedId, fromLat, fromLon, mode);
}
