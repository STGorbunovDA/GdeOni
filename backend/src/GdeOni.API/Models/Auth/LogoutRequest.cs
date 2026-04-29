namespace GdeOni.API.Models.Auth;

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = null!;
}
