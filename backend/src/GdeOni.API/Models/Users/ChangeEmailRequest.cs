namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос смены email текущего пользователя. Операция ротирует
/// SecurityStamp и инвалидирует выданные access-токены.
/// </summary>
public sealed class ChangeEmailRequest
{
    /// <summary>Новый email пользователя.</summary>
    public string Email { get; set; } = string.Empty;
}
