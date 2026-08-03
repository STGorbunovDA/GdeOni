using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Relatives;

namespace GdeOni.Application.Relatives.Common;

/// <summary>
/// Domain → Response для экрана диалога. Удалённые сообщения выкидываем;
/// CanEditDelete считаем от последнего видимого (turn-based). Порядок — по
/// времени создания.
/// </summary>
public static class RelativeConversationMapper
{
    public static RelativeConversationDetailResponse ToDetail(
        RelativeConversation conversation,
        RelativeConversationHeader header,
        Guid currentUserId)
    {
        var otherId = conversation.OtherParticipant(currentUserId);
        var lastVisible = conversation.LastVisibleMessage();

        var messages = conversation.Messages
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new RelativeMessageResponse(
                m.Id,
                m.SenderId == currentUserId,
                m.Text,
                m.CreatedAtUtc,
                m.EditedAtUtc,
                m.IsRead,
                m.SenderId == currentUserId
                    && lastVisible is not null
                    && lastVisible.Id == m.Id))
            .ToList();

        return new RelativeConversationDetailResponse(
            conversation.Id,
            conversation.DeceasedId,
            header.DeceasedFullName,
            otherId,
            header.OtherUserName,
            header.OtherRelationship?.ToString(),
            conversation.CanSend(currentUserId),
            messages);
    }
}
