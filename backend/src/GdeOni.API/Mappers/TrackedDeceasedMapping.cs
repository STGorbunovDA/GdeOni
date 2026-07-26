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

/// <summary>
/// Request → Command/Query маппинг для контроллеров отслеживания
/// умерших и построения маршрутов до могил.
/// </summary>
public static class TrackedDeceasedMapping
{
    /// <summary>Маппит DTO пагинации в запрос списка отслеживаемых умерших.</summary>
    public static GetMyTrackedDeceasedListQuery ToQuery(this GetMyTrackedDeceasedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetMyTrackedDeceasedListQuery(request.Page, request.PageSize);
    }

    /// <summary>Возвращает запрос детальной информации об отслеживаемой карточке.</summary>
    public static GetMyTrackedDeceasedDetailsQuery ToDetailsQuery(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Возвращает запрос проверки факта отслеживания карточки текущим пользователем.</summary>
    public static IsTrackedByMeQuery ToIsTrackedByMeQuery(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Маппит DTO добавления в отслеживание в команду use case.</summary>
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

    /// <summary>Маппит DTO правки настроек отслеживания в команду use case.</summary>
    public static UpdateTrackingCommand ToCommand(this UpdateTrackingRequest request, Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateTrackingCommand(
            deceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            ResolveLeadDays(request.DeathAnniversaryLeadDays, request.NotifyOnDeathAnniversary),
            ResolveLeadDays(request.BirthAnniversaryLeadDays, request.NotifyOnBirthAnniversary),
            request.TrackStatus);
    }

    /// <summary>
    /// F42. Набор дней напоминания: если новый клиент прислал список — берём
    /// его; иначе (старый клиент) выводим из булева флага: true → «в день» (0),
    /// false → выключено.
    /// </summary>
    private static IReadOnlyList<int> ResolveLeadDays(IReadOnlyList<int>? leadDays, bool legacyFlag)
    {
        if (leadDays is not null)
            return leadDays;

        return legacyFlag ? new[] { 0 } : Array.Empty<int>();
    }

    /// <summary>Возвращает команду удаления карточки из персонального отслеживания.</summary>
    public static RemoveTrackingCommand ToRemoveCommand(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Возвращает запрос построения маршрута до могилы.</summary>
    public static GetRouteToGraveQuery ToRouteQuery(
        Guid deceasedId,
        double fromLat,
        double fromLon,
        RoutingMode mode)
        => new(deceasedId, fromLat, fromLon, mode);
}
