namespace GdeOni.Application.Relatives.Queries.GetConversation.Model;

/// <summary>Открыть диалог по id (проверка участника + отметка прочтения).</summary>
public sealed record GetConversationQuery(Guid ConversationId);
