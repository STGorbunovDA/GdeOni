namespace GdeOni.Application.Users.Commands.AssignMissingLogins.Model;

/// <summary>
/// Массово проставить логины пользователям, у которых логина нет.
/// Параметров нет — правило формирования одно и то же для всех.
/// </summary>
public sealed record AssignMissingLoginsCommand;

/// <summary>Сколько пользователей получили логин.</summary>
public sealed record AssignMissingLoginsResponse(int AssignedCount);
