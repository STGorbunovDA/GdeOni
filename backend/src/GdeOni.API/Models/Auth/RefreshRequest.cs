namespace GdeOni.API.Models.Auth;

/// <summary>
/// Запрос обновления access-токена по refresh-токену.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>Действующий refresh-токен.</summary>
    public string RefreshToken { get; set; } = null!;
}
