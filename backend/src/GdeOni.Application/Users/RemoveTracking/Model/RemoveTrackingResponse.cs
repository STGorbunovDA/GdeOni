namespace GdeOni.Application.Users.RemoveTracking.Model;

public sealed class RemoveTrackingResponse
{
    public Guid UserId { get; init; }
    public Guid DeceasedId { get; init; }
    public bool Removed { get; init; }
}