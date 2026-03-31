namespace GdeOni.Application.Users.GetAll.Model;

public sealed class GetAllUsersResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string? FullName { get; init; }
    public string Role { get; init; } = null!;
    public DateTime RegisteredAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public int TrackingCount { get; init; }
}