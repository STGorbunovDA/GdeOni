using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

public sealed class GetAllUsersRequest
{
    public string? Search { get; set; }
    public UserRole? Role { get; init; }
    public DateTime? RegisteredAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? RegisteredFromUtc { get; init; }
    public DateTime? RegisteredToUtc { get; init; }
}