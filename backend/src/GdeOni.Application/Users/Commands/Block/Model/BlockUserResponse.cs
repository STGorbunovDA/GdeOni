namespace GdeOni.Application.Users.Commands.Block.Model;

public sealed record BlockUserResponse(Guid UserId, DateTime BlockedAtUtc);
