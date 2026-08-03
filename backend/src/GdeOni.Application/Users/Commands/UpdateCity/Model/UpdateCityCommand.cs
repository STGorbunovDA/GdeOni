namespace GdeOni.Application.Users.Commands.UpdateCity.Model;

/// <summary>
/// Указать/сменить город текущего пользователя. null или пустая строка —
/// «не указан» (домен нормализует в null).
/// </summary>
public sealed record UpdateCityCommand(string? City);
