namespace GdeOni.Application.Users.Create.Model;

public sealed class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public string? FullName { get; set; }

    // Важно: доменная модель сейчас принимает именно PasswordHash.
    // TODO отдельно добавиnm IPasswordHasher.
    public string PasswordHash { get; set; } = null!;
}