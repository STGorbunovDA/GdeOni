namespace GdeOni.API.Models.Auth;

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = null!;
}
