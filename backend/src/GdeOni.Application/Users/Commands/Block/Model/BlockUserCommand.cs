namespace GdeOni.Application.Users.Commands.Block.Model;

public sealed record BlockUserCommand(Guid UserId, string? Reason);
