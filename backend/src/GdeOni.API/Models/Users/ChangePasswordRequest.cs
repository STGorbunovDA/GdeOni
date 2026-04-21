namespace GdeOni.API.Models.Users;

public sealed class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string NewPassword { get; set; } = null!;
}