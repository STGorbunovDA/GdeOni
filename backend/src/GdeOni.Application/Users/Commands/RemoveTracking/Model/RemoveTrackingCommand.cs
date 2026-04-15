namespace GdeOni.Application.Users.Commands.RemoveTracking.Model;

public sealed record RemoveTrackingCommand(Guid UserId, Guid DeceasedId);