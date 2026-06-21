namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос входа в систему: email и пароль.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>Email пользователя.</summary>
    public string Email { get; set; } = null!;
    /// <summary>Пароль пользователя.</summary>
    public string Password { get; set; } = null!;
}
