namespace GdeOni.Application.Users.Commands.Register.Model;

public sealed record RegisterUserCommand(
    string Email,
    string? UserName,
    string? FullName,
    string Password);