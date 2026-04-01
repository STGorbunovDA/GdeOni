namespace GdeOni.Application.Users.HasNotificationsEnabled.Model;

public sealed class HasNotificationsEnabledResponse
{
    public Guid UserId { get; init; }
    public Guid DeceasedId { get; init; }
    public bool HasNotificationsEnabled { get; init; }
}