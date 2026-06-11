namespace GdeOni.Application.Users.Commands.UpdateProfile.Model;

/// <summary>
/// CurrentPassword опционален для backward-compat (старые клиенты не
/// шлют). Если задан — обязан совпасть с user.PasswordHash. Если null
/// и target != currentUser (то есть админ правит чужой профиль) —
/// пропускается, у админа уже есть права.
/// </summary>
public record UpdateUserProfileCommand(
    Guid UserId,
    string UserName,
    string? FullName,
    string? CurrentPassword = null);