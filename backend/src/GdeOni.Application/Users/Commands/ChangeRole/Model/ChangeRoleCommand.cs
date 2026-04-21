using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeRole.Model;

public record ChangeRoleCommand(Guid UserId, UserRole UserRole);