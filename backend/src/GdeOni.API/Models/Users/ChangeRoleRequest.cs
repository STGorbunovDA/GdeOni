using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос смены роли пользователя (админская операция).
/// </summary>
public sealed class ChangeRoleRequest
{
    /// <summary>Новая роль пользователя.</summary>
    public UserRole UserRole { get; set; }
}
