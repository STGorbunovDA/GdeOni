namespace GdeOni.Application.Relatives.Commands.SendMessage.Model;

/// <summary>Отправить сообщение в диалог. Разрешено только когда сейчас твой ход.</summary>
public sealed record SendMessageCommand(Guid ConversationId, string Text);
