namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос смены пароля пользователя. Операция ротирует SecurityStamp
/// и инвалидирует выданные access-токены.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>Текущий пароль (для подтверждения операции).</summary>
    public string? CurrentPassword { get; set; }
    /// <summary>Новый пароль.</summary>
    public string NewPassword { get; set; } = null!;
}
