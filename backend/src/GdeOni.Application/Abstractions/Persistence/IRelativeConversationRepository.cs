using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>Шапка диалога для экрана переписки: с кем и по какой карточке.</summary>
public sealed record RelativeConversationHeader(
    string DeceasedFullName,
    string OtherUserName,
    RelationshipType? OtherRelationship);

/// <summary>Строка списка диалогов (инбокс + непрочитанные для уведомлений).</summary>
public sealed record RelativeConversationSummary(
    Guid ConversationId,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid OtherUserId,
    string OtherUserName,
    RelationshipType? OtherRelationship,
    DateTime LastMessageAtUtc,
    string? LastMessagePreview,
    bool LastMessageIsMine,
    int UnreadCount,
    bool CanSend);

public interface IRelativeConversationRepository
{
    Task Add(RelativeConversation conversation, CancellationToken cancellationToken);

    /// <summary>Диалог с загруженными сообщениями (tracked — для мутаций).</summary>
    Task<RelativeConversation?> GetByIdWithMessages(Guid id, CancellationToken cancellationToken);

    /// <summary>Существующий диалог пары в контексте карточки (для get-or-create).</summary>
    Task<RelativeConversation?> GetByParticipants(
        Guid deceasedId, Guid userX, Guid userY, CancellationToken cancellationToken);

    Task<RelativeConversationHeader?> GetHeader(
        Guid deceasedId, Guid otherUserId, CancellationToken cancellationToken);

    Task<List<RelativeConversationSummary>> GetConversationsForUser(
        Guid userId, CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
