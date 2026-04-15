using GdeOni.Domain.Shared;

namespace GdeOni.API.Models;

public sealed class ChangeRoleDto
{
    public UserRole UserRole { get; set; }
}