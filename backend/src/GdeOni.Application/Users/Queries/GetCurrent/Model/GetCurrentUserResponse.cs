namespace GdeOni.Application.Users.Queries.GetCurrent.Model;

public sealed class GetCurrentUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string? FullName { get; init; }
    public string Role { get; init; } = null!;
}
