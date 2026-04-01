using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeUserRole.Model;

public sealed class ChangeUserRoleRequest
{
    public Guid UserId { get; set; }
    public UserRole UserRole { get; set; }
}