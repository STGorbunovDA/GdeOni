namespace GdeOni.Application.Users.Commands.ChangeEmail.Model;

public record ChangeEmailCommand(Guid UserId, string Email);