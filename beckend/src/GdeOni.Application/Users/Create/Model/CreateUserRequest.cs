namespace GdeOni.Application.Users.Create.Model;

public sealed class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string Password { get; set; } = null!;
}