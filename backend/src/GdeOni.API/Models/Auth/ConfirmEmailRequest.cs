namespace GdeOni.API.Models.Auth;

/// <summary>
/// D45. Подтверждение email по токену из письма.
/// </summary>
public sealed class ConfirmEmailRequest
{
    /// <summary>Токен из ссылки в письме.</summary>
    public string Token { get; set; } = null!;
}
