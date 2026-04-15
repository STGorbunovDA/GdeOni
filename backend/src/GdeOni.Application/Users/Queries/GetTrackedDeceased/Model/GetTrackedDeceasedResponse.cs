namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;

public sealed class GetTrackedDeceasedResponse
{
    public IReadOnlyCollection<TrackedDeceasedItemResponse> Items { get; init; } = [];
}