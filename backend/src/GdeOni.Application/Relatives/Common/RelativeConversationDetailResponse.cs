namespace GdeOni.Application.Relatives.Common;

/// <summary>
/// Одно сообщение в переписке. CanEditDelete = true только у СВОЕГО последнего
/// видимого сообщения, пока собеседник не ответил (turn-based правило).
/// Удалённые сообщения в список не попадают.
/// </summary>
public sealed record RelativeMessageResponse(
    Guid Id,
    bool IsMine,
    string Text,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc,
    bool IsRead,
    bool CanEditDelete);

/// <summary>
/// Полный экран диалога: с кем (ник + связь), по какой карточке, чей сейчас
/// ход (CanSend) и список сообщений.
/// </summary>
public sealed record RelativeConversationDetailResponse(
    Guid ConversationId,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherRelationship,
    bool CanSend,
    IReadOnlyList<RelativeMessageResponse> Messages);
