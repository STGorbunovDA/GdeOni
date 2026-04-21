namespace GdeOni.API.Models.Users;

public sealed class UpdateUserProfileRequest
{
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
}