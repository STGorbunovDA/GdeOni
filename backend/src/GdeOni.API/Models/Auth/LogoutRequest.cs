namespace GdeOni.API.Models.Auth;

/// <summary>
/// Запрос выхода из системы: серверная ревокация переданного
/// refresh-токена.
/// </summary>
public sealed class LogoutRequest
{
    /// <summary>Refresh-токен, который нужно отозвать.</summary>
    public string RefreshToken { get; set; } = null!;
}
