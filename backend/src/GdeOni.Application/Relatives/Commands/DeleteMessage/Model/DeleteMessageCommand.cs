namespace GdeOni.Application.Relatives.Commands.DeleteMessage.Model;

/// <summary>
/// Удалить своё последнее сообщение — можно, пока собеседник не ответил.
/// После удаления ход возвращается автору.
/// </summary>
public sealed record DeleteMessageCommand(Guid ConversationId, Guid MessageId);
