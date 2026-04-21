namespace GdeOni.Application.Users.Commands.ChangePassword.Model;

public record ChangePasswordCommand(Guid UserId, string? CurrentPassword, string NewPassword);