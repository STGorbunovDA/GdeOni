using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetAll.Model;

public sealed class GetAllUsersQuery
{
    public string? Search { get; set; }
    public UserRole? Role { get; init; }
    public DateTime? RegisteredAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}