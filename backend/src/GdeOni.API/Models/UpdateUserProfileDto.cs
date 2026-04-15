namespace GdeOni.API.Models;

public sealed class UpdateUserProfileDto
{
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
}