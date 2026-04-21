namespace GdeOni.Application.Users.Commands.ChangeEmail.Model;

public sealed record ChangeEmailResponse(Guid UserId, string Email);