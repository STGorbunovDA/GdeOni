namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;

public sealed record GetTrackedDeceasedResponse(
    IReadOnlyCollection<TrackedDeceasedItemResponse> Items);