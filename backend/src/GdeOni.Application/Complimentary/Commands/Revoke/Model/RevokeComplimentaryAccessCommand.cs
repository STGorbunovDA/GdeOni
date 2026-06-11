namespace GdeOni.Application.Complimentary.Commands.Revoke.Model;

/// <summary>
/// D22. Команда: админ отзывает бесплатный доступ у юзера.
/// </summary>
public sealed record RevokeComplimentaryAccessCommand(Guid TargetUserId);
