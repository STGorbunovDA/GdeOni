namespace GdeOni.Application.Auth.Login.Model;

public sealed class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}