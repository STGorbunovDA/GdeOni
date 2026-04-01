namespace GdeOni.Application.Users.ChangePassword.Model;

public sealed class ChangePasswordRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}