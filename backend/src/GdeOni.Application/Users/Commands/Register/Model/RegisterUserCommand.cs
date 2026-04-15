namespace GdeOni.Application.Users.Commands.Register.Model;

public sealed class RegisterUserCommand
{
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string Password { get; set; } = null!;
}