namespace GdeOni.Application.Relatives.Commands.EditMessage.Model;

/// <summary>
/// Изменить своё последнее сообщение — можно, пока собеседник не ответил.
/// </summary>
public sealed record EditMessageCommand(Guid ConversationId, Guid MessageId, string Text);
