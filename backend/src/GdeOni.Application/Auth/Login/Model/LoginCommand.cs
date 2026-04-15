namespace GdeOni.Application.Auth.Login.Model;

public sealed class LoginCommand
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}