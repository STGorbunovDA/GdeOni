namespace GdeOni.Application.Users.IsTracking.Model;

public sealed class IsTrackingResponse
{
    public Guid UserId { get; init; }
    public Guid DeceasedId { get; init; }
    public bool IsTracking { get; init; }
}