namespace GdeOni.Application.Support.Commands.AddUserMessage.Model;

/// <summary>
/// D44. Сообщение от пользователя в переписку своего обращения.
/// Текст обязателен — пустой «пузырь» в чате смысла не имеет
/// (в отличие от Reopen, где текст опционален).
/// </summary>
public sealed record AddUserMessageCommand(Guid TicketId, string Text);
