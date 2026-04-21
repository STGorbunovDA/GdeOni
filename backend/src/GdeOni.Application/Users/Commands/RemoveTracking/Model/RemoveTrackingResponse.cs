namespace GdeOni.Application.Users.Commands.RemoveTracking.Model;

public sealed class RemoveTrackingResponse
{
    public Guid UserId { get; init; }
    public Guid DeceasedId { get; init; }
    public bool Removed { get; init; }
}