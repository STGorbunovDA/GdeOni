namespace GdeOni.Application.Relatives.Queries.ListConversations.Model;

/// <summary>Строка списка диалогов: с кем, по какой карточке, превью и непрочитанные.</summary>
public sealed record RelativeConversationSummaryResponse(
    Guid ConversationId,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherRelationship,
    DateTime LastMessageAtUtc,
    string? LastMessagePreview,
    bool LastMessageIsMine,
    int UnreadCount,
    bool CanSend);

public sealed record ListRelativeConversationsResponse(
    IReadOnlyList<RelativeConversationSummaryResponse> Items);
