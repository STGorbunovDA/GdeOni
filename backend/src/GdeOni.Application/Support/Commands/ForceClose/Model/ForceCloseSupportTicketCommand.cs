namespace GdeOni.Application.Support.Commands.ForceClose.Model;

/// <summary>
/// D40. Админ закрывает обращение принудительно, из любого статуса.
///
/// <c>CloseNote</c> обязателен: закрытие «через голову» пользователя надо
/// объяснить — причина уходит в переписку отдельным сообщением от админа.
/// </summary>
public record ForceCloseSupportTicketCommand(
    Guid TicketId,
    string CloseNote);
