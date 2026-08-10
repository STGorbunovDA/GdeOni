namespace GdeOni.Application.Complimentary.Commands.GrantToAll.Model;

/// <summary>
/// Массовая выдача бесплатного (комплиментарного) доступа ВСЕМ пользователям.
/// DurationDays = null → 30 дней по умолчанию.
/// </summary>
public sealed record GrantComplimentaryAccessToAllCommand(int? DurationDays);

/// <summary>
/// Результат массовой выдачи: сколько пользователей затронуто и до какой
/// даты выдан доступ.
/// </summary>
public sealed record GrantComplimentaryAccessToAllResponse(int AffectedCount, DateTime UntilUtc);
