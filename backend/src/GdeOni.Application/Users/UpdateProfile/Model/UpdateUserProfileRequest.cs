namespace GdeOni.Application.Users.UpdateProfile.Model;

public sealed class UpdateUserProfileRequest
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
}