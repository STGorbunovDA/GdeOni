namespace GdeOni.API.Models.Auth;

/// <summary>
/// D45. Повторная отправка письма с подтверждением email.
/// </summary>
public sealed class ResendConfirmationRequest
{
    /// <summary>Адрес, на который отправить письмо.</summary>
    public string Email { get; set; } = null!;
}
