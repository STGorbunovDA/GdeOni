namespace GdeOni.Application.Relatives.Commands.StartConversation.Model;

/// <summary>
/// Функция «Родственники»: открыть (или получить существующий) диалог с
/// родственником по карточке. Инициатор — текущий пользователь.
/// </summary>
public sealed record StartConversationCommand(Guid DeceasedId, Guid OtherUserId);
