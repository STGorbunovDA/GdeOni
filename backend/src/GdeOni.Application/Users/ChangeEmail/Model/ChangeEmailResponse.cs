namespace GdeOni.Application.Users.ChangeEmail.Model;

public sealed record ChangeEmailResponse(Guid UserId, string Email);