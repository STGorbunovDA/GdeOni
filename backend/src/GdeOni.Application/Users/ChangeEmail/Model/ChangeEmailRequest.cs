namespace GdeOni.Application.Users.ChangeEmail.Model;

public sealed class ChangeEmailRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}