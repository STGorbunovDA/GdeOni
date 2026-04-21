namespace GdeOni.Application.Users.Commands.UpdateProfile.Model;

public record UpdateUserProfileCommand (Guid UserId, string UserName, string? FullName);