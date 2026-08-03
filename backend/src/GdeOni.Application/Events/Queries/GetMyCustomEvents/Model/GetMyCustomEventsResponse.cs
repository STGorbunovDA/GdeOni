using GdeOni.Application.Events.Common;

namespace GdeOni.Application.Events.Queries.GetMyCustomEvents.Model;

public sealed record GetMyCustomEventsResponse(IReadOnlyList<CustomEventDto> Items);
