namespace GdeOni.Application.Users.GetTrackedDeceased.Model;

public sealed class GetTrackedDeceasedResponse
{
    public IReadOnlyCollection<TrackedDeceasedItemResponse> Items { get; init; } = [];
}