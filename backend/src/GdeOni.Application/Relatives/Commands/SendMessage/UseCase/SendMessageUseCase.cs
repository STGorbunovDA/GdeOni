using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.SendMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.SendMessage.UseCase;

/// <summary>
/// Отправляет сообщение (если сейчас ход пользователя). Перед отправкой
/// помечает прочитанным сообщение собеседника — отвечая, ты его прочитал.
/// </summary>
public sealed class SendMessageUseCase(
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : ISendMessageUseCase
{
    public async Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        SendMessageCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;
        var conversation = await conversationRepository.GetByIdWithMessages(
            command.ConversationId, cancellationToken);
        if (conversation is null)
            return Errors.Relatives.ConversationNotFound();

        if (!conversation.IsParticipant(me))
            return Errors.Relatives.NotParticipant();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        conversation.MarkReadBy(me, now);

        var sendResult = conversation.SendMessage(me, command.Text, now);
        if (sendResult.IsFailure)
            return sendResult.Error;

        await conversationRepository.Save(cancellationToken);

        var header = await conversationRepository.GetHeader(
            conversation.DeceasedId, conversation.OtherParticipant(me), cancellationToken);
        if (header is null)
            return Errors.Relatives.ConversationNotFound();

        return RelativeConversationMapper.ToDetail(conversation, header, me);
    }
}
