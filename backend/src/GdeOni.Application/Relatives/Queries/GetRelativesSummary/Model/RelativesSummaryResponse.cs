namespace GdeOni.Application.Relatives.Queries.GetRelativesSummary.Model;

/// <summary>
/// Фаза 4. Сводка для попапа «События» и бейджа вкладки «Родственники»:
/// новые родственники (обнаружены ночным джобом, ещё не просмотрены) +
/// непрочитанные диалоги (считаются вживую).
/// </summary>
public sealed record RelativesSummaryResponse(
    IReadOnlyList<NewRelativeSummaryItem> NewRelatives,
    IReadOnlyList<UnreadConversationItem> UnreadConversations,
    int TotalUnreadMessages);

/// <summary>Новый родственник: кем приходится и по какой карточке.</summary>
public sealed record NewRelativeSummaryItem(
    Guid DeceasedId,
    string DeceasedFullName,
    Guid RelativeUserId,
    string RelativeUserName,
    string RelationshipType);

/// <summary>Диалог с непрочитанными сообщениями от собеседника.</summary>
public sealed record UnreadConversationItem(
    Guid ConversationId,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid OtherUserId,
    string OtherUserName,
    int UnreadCount);
