namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос блокировки пользователя администратором.
/// </summary>
public sealed class BlockUserRequest
{
    /// <summary>Причина блокировки (отображается в админке).</summary>
    public string? Reason { get; set; }
}
