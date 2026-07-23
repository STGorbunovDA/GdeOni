namespace GdeOni.API.Models.Auth;

/// <summary>
/// D43. Запрос ссылки восстановления пароля.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>Email аккаунта, к которому потерян доступ.</summary>
    public string Email { get; set; } = null!;
}
