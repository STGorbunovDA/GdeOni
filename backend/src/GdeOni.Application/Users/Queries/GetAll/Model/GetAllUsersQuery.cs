using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetAll.Model;

public sealed record GetAllUsersQuery(
    string? Search,
    UserRole? Role,
    DateTime? RegisteredAtUtc,
    DateTime? LastLoginAtUtc,
    int Page = 1,
    int PageSize = 20);