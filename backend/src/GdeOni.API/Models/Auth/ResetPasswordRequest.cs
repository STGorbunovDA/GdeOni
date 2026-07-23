namespace GdeOni.API.Models.Auth;

/// <summary>
/// D43. Установка нового пароля по токену из письма.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>Токен из ссылки в письме.</summary>
    public string Token { get; set; } = null!;

    /// <summary>Новый пароль.</summary>
    public string NewPassword { get; set; } = null!;
}
