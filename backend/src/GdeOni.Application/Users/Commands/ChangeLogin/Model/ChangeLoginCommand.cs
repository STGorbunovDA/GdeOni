namespace GdeOni.Application.Users.Commands.ChangeLogin.Model;

/// <summary>
/// Смена собственного логина в профиле. Уникальность проверяется в use case:
/// два одинаковых логина в системе невозможны.
/// </summary>
public sealed record ChangeLoginCommand(string Login);
