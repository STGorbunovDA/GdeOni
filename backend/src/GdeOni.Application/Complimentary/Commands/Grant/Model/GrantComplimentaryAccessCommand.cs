namespace GdeOni.Application.Complimentary.Commands.Grant.Model;

/// <summary>
/// D22. Команда: админ выдаёт бесплатный доступ ко всему приложению
/// конкретному юзеру. <paramref name="UntilUtc"/> = null → бессрочно.
/// </summary>
public sealed record GrantComplimentaryAccessCommand(
    Guid TargetUserId,
    DateTime? UntilUtc,
    string? Note);
