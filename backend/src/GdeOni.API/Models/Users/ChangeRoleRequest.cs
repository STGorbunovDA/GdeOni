using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

public sealed class ChangeRoleRequest
{
    public UserRole UserRole { get; set; }
}